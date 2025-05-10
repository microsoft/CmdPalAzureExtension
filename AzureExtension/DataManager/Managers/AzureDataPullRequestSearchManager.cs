// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Account;
using AzureExtension.Client;
using AzureExtension.Controls;
using AzureExtension.Data;
using AzureExtension.DataModel;
using Microsoft.TeamFoundation.Policy.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Serilog;
using PullRequestSearch = AzureExtension.DataModel.PullRequestSearch;

namespace AzureExtension.DataManager;

public class AzureDataPullRequestSearchManager
    : ISearchDataProvider<IPullRequestSearch, PullRequestSearch>, IContentDataProvider<IPullRequestSearch, PullRequest>, IDataUpdater, IContentDataProvider, ISearchDataProvider
{
    private readonly TimeSpan _pullRequestSearchDeletionTime = TimeSpan.FromMinutes(2);

    private readonly ILogger _log;
    private readonly DataStore _dataStore;
    private readonly IAccountProvider _accountProvider;
    private readonly IAzureLiveDataProvider _liveDataProvider;
    private readonly IConnectionProvider _connectionProvider;
    private readonly ISavedSearchesSource<IPullRequestSearch> _pullRequestSearchRepository;

    public AzureDataPullRequestSearchManager(
        DataStore dataStore,
        IAccountProvider accountProvider,
        IAzureLiveDataProvider liveDataProvider,
        IConnectionProvider connectionProvider,
        ISavedSearchesSource<IPullRequestSearch> pullRequestSearchRepository)
    {
        _dataStore = dataStore;
        _accountProvider = accountProvider;
        _log = Log.ForContext("SourceContext", nameof(AzureDataPullRequestSearchManager));
        _liveDataProvider = liveDataProvider;
        _connectionProvider = connectionProvider;
        _pullRequestSearchRepository = pullRequestSearchRepository;
    }

    private void ValidateDataStore()
    {
        if (_dataStore == null || !_dataStore.IsConnected)
        {
            throw new DataStoreInaccessibleException("Cache DataStore is not available.");
        }
    }

    public PullRequestSearch? GetDataForSearch(IPullRequestSearch pullRequestSearch)
    {
        ValidateDataStore();
        var account = _accountProvider.GetDefaultAccount();
        var azureUri = new AzureUri(pullRequestSearch.Url);
        return PullRequestSearch.Get(
            _dataStore,
            azureUri.Organization,
            azureUri.Project,
            azureUri.Repository,
            account.Username,
            GetPullRequestView(pullRequestSearch.View));
    }

    public IEnumerable<PullRequest> GetDataObjects(IPullRequestSearch pullRequestSearch)
    {
        ValidateDataStore();
        var dsPullRequestSearch = GetDataForSearch(pullRequestSearch);
        return dsPullRequestSearch != null ? PullRequest.GetForPullRequestSearch(_dataStore, dsPullRequestSearch!) : [];
    }

    public IEnumerable<object> GetDataObjects(IAzureSearch search)
    {
        return GetDataObjects(search as IPullRequestSearch ?? throw new InvalidOperationException("Invalid search type"));
    }

    public object? GetDataForSearch(IAzureSearch search)
    {
        return GetDataForSearch(search as IPullRequestSearch ?? throw new InvalidOperationException("Invalid search type"));
    }

    public bool IsNewOrStale(IPullRequestSearch pullRequestSearch, TimeSpan refreshCooldown)
    {
        var dsPullRequestSearch = GetDataForSearch(pullRequestSearch);
        return dsPullRequestSearch == null || DateTime.UtcNow - dsPullRequestSearch.UpdatedAt > refreshCooldown;
    }

    public bool IsNewOrStale(DataUpdateParameters parameters, TimeSpan refreshCooldown)
    {
        return IsNewOrStale((parameters.UpdateObject as IPullRequestSearch)!, refreshCooldown);
    }

    public async Task UpdatePullRequestsAsync(IPullRequestSearch pullRequestSearch, CancellationToken cancellationToken)
    {
        var azureUri = new AzureUri(pullRequestSearch.Url);

        var org = Organization.GetOrCreate(_dataStore, azureUri.Connection);

        var project = Project.Get(_dataStore, azureUri.Project, org.Id);
        var account = await _accountProvider.GetDefaultAccountAsync();
        var vssConnection = await _connectionProvider.GetVssConnectionAsync(azureUri.Connection, account);

        if (project is null)
        {
            var teamProject = await _liveDataProvider.GetTeamProject(vssConnection, azureUri.Project);
            project = Project.GetOrCreateByTeamProject(_dataStore, teamProject, org.Id);
        }

        var gitRepository = await _liveDataProvider.GetRepositoryAsync(vssConnection, project.InternalId, azureUri.Repository, cancellationToken);

        var searchCriteria = new GitPullRequestSearchCriteria
        {
            Status = PullRequestStatus.Active,
            IncludeLinks = true,
        };

        var authorizedEntityId = vssConnection.AuthorizedIdentity.Id;

        switch (GetPullRequestView(pullRequestSearch.View))
        {
            case PullRequestView.Unknown:
                throw new ArgumentException("PullRequestView is unknown");
            case PullRequestView.Mine:
                searchCriteria.CreatorId = authorizedEntityId;
                break;
            case PullRequestView.Assigned:
                searchCriteria.ReviewerId = authorizedEntityId;
                break;
            case PullRequestView.All:
                /* Nothing different for this */
                break;
        }

        // Get the pull requests with those criteria: (do we need internal id)
        var pullRequests = await _liveDataProvider.GetPullRequestsAsync(vssConnection, project.InternalId, gitRepository.Id, searchCriteria, cancellationToken);

        var repository = Repository.GetOrCreate(_dataStore, gitRepository, project.Id);

        var dsPullRequestSearch = PullRequestSearch.GetOrCreate(_dataStore, repository.Id, project.Id, account.Username, GetPullRequestView(pullRequestSearch.View));

        using var dbSemaphore = new SemaphoreSlim(1, 1);

        var tasks = new List<Task<PullRequest>>();
        foreach (var pullRequest in pullRequests)
        {
            var prTask = Task.Run(async () =>
            {
                var status = PolicyStatus.Unknown;
                var statusReason = string.Empty;

                // ArtifactId is null in the pull request object and it is not the correct object. The ArtifactId for the
                // Policy Evaluations API is this:
                //     vstfs:///CodeReview/CodeReviewId/{projectId}/{pullRequestId}
                // Documentation: https://learn.microsoft.com/en-us/dotnet/api/microsoft.teamfoundation.policy.webapi.policyevaluationrecord.artifactid
                var artifactId = $"vstfs:///CodeReview/CodeReviewId/{project.InternalId}/{pullRequest.PullRequestId}";

                var policyEvaluationsTask = _liveDataProvider.GetPolicyEvaluationsAsync(vssConnection, project.InternalId, artifactId, cancellationToken);
                Task<GitCommit>? commitTask = null;
                if (pullRequest.LastMergeSourceCommit is not null)
                {
                    commitTask = _liveDataProvider.GetCommitAsync(vssConnection, pullRequest.LastMergeSourceCommit.CommitId, gitRepository.Id, cancellationToken);
                }

                try
                {
                    var policyEvaluations = await Task.WhenAny(policyEvaluationsTask, Task.Delay(TimeSpan.FromSeconds(5))) == policyEvaluationsTask
                                           ? await policyEvaluationsTask
                                           : throw new TimeoutException("Fetching policy evaluations timed out.");
                    GetPolicyStatus(policyEvaluations, out status, out statusReason);
                }
                catch (Exception ex)
                {
                    _log.Error(ex, $"Failed getting policy evaluations for pull request: {pullRequest.PullRequestId} {pullRequest.Url}");
                }

                if (pullRequest.LastMergeSourceCommit is not null)
                {
                    var commitRef = await commitTask!;
                    if (commitRef is not null)
                    {
                        pullRequest.LastMergeSourceCommit = commitRef;
                    }
                }

                await dbSemaphore.WaitAsync(cancellationToken);
                try
                {
                    var creator = Identity.GetOrCreateIdentity(_dataStore, pullRequest.CreatedBy, vssConnection, _liveDataProvider);
                    var dsPullRequest = PullRequest.GetOrCreate(_dataStore, pullRequest, repository.Id, creator.Id, status, statusReason);
                    return dsPullRequest;
                }
                finally
                {
                    dbSemaphore.Release();
                }
            });

            tasks.Add(prTask);
        }

        foreach (var task in tasks)
        {
            var dsPullRequest = await task;
            await dbSemaphore.WaitAsync(cancellationToken);
            try
            {
                PullRequestSearchPullRequest.AddPullRequestToSearch(_dataStore, dsPullRequestSearch.Id, dsPullRequest.Id);
            }
            finally
            {
                dbSemaphore.Release();
            }
        }

        PullRequestSearchPullRequest.DeleteBefore(_dataStore, dsPullRequestSearch, DateTime.UtcNow - _pullRequestSearchDeletionTime);
    }

    // Helper methods
    private PullRequestView GetPullRequestView(string viewStr)
    {
        try
        {
            return Enum.Parse<PullRequestView>(viewStr);
        }
        catch (Exception)
        {
            _log.Error($"Unknown Pull Request view for string: {viewStr}");
            return PullRequestView.Unknown;
        }
    }

    // Gets PolicyStatus and reason for a given list of PolicyEvaluationRecords
    private void GetPolicyStatus(List<PolicyEvaluationRecord> policyEvaluations, out PolicyStatus status, out string statusReason)
    {
        status = PolicyStatus.Unknown;
        statusReason = string.Empty;

        if (policyEvaluations != null)
        {
            var countApplicablePolicies = 0;
            foreach (var policyEvaluation in policyEvaluations)
            {
                if (policyEvaluation.Configuration.IsEnabled && policyEvaluation.Configuration.IsBlocking)
                {
                    ++countApplicablePolicies;
                    var evalStatus = PullRequestPolicyStatus.GetFromPolicyEvaluationStatus(policyEvaluation.Status);
                    if (evalStatus < status)
                    {
                        statusReason = policyEvaluation.Configuration.Type.DisplayName;
                        status = evalStatus;
                    }
                }
            }

            if (countApplicablePolicies == 0)
            {
                // If there is no applicable policy, treat the policy status as Approved.
                status = PolicyStatus.Approved;
            }
        }
    }

    private readonly TimeSpan _pullRequestSearchRetentionTime = TimeSpan.FromDays(7);

    public void PruneObsoleteData()
    {
        PullRequestSearch.DeleteBefore(_dataStore, DateTime.UtcNow - _pullRequestSearchRetentionTime);
        PullRequest.DeleteNotReferencedBySearch(_dataStore);
        PullRequestSearchPullRequest.DeleteUnreferenced(_dataStore);
    }

    public async Task UpdateData(DataUpdateParameters parameters)
    {
        if (parameters.UpdateType == DataUpdateType.All)
        {
            var pullRequestSearches = _pullRequestSearchRepository.GetSavedSearches();
            foreach (var pullRequestSearch in pullRequestSearches)
            {
                await UpdatePullRequestsAsync(pullRequestSearch, parameters.CancellationToken.GetValueOrDefault());
            }

            return;
        }

        await UpdatePullRequestsAsync((parameters.UpdateObject as IPullRequestSearch)!, parameters.CancellationToken.GetValueOrDefault());
    }
}
