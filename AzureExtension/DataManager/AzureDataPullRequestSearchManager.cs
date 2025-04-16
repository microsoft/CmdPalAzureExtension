// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Account;
using AzureExtension.Client;
using AzureExtension.Controls;
using AzureExtension.Data;
using AzureExtension.DataModel;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.Policy.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Serilog;
using PullRequestSearch = AzureExtension.DataModel.PullRequestSearch;

namespace AzureExtension.DataManager;

public class AzureDataPullRequestSearchManager : IDataPullRequestSearchUpdater, IDataPullRequestSearchProvider
{
    private readonly ILogger _log;
    private readonly DataStore _dataStore;
    private readonly IAccountProvider _accountProvider;
    private readonly AzureClientProvider _azureClientProvider;

    public AzureDataPullRequestSearchManager(DataStore dataStore, IAccountProvider accountProvider, AzureClientProvider clientProvider)
    {
        _dataStore = dataStore;
        _accountProvider = accountProvider;
        _log = Log.ForContext("SourceContext", nameof(AzureDataPullRequestSearchManager));
        _azureClientProvider = clientProvider;
    }

    private void ValidateDataStore()
    {
        if (_dataStore == null || !_dataStore.IsConnected)
        {
            throw new DataStoreInaccessibleException("Cache DataStore is not available.");
        }
    }

    public PullRequestSearch? GetPullRequestSearch(IPullRequestSearch pullRequestSearch)
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

    public IEnumerable<IPullRequest> GetPullRequests(IPullRequestSearch pullRequestSearch)
    {
        ValidateDataStore();
        var dsPullRequestSearch = GetPullRequestSearch(pullRequestSearch);
        return dsPullRequestSearch != null ? PullRequest.GetForPullRequestSearch(_dataStore, dsPullRequestSearch!) : [];
    }

    public bool IsNewOrStale(IPullRequestSearch pullRequestSearch, TimeSpan refreshCooldown)
    {
        var dsPullRequestSearch = GetPullRequestSearch(pullRequestSearch);
        return dsPullRequestSearch == null || DateTime.UtcNow - dsPullRequestSearch.UpdatedAt > refreshCooldown;
    }

    public async Task UpdatePullRequestsAsync(IPullRequestSearch pullRequestSearch, CancellationToken cancellationToken)
    {
        var azureUri = new AzureUri(pullRequestSearch.Url);
        var account = _accountProvider.GetDefaultAccount();
        var connection = _azureClientProvider.GetVssConnection(azureUri.Connection, account);

        using var gitClient = _azureClientProvider.GetClient<GitHttpClient>(azureUri.Connection, account);

        var org = Organization.GetOrCreate(_dataStore, azureUri.Connection);

        var project = Project.Get(_dataStore, azureUri.Project, org.Id);

        if (project is null)
        {
            using var projectClient = _azureClientProvider.GetClient<ProjectHttpClient>(azureUri.Connection, account);
            var teamProject = await projectClient.GetProject(azureUri.Project);
            project = Project.GetOrCreateByTeamProject(_dataStore, teamProject, org.Id);
        }

        var gitRepository = await gitClient.GetRepositoryAsync(project.InternalId, azureUri.Repository, cancellationToken: cancellationToken);

        var searchCriteria = new GitPullRequestSearchCriteria
        {
            Status = PullRequestStatus.Active,
            IncludeLinks = true,
        };

        switch (GetPullRequestView(pullRequestSearch.View))
        {
            case PullRequestView.Unknown:
                throw new ArgumentException("PullRequestView is unknown");
            case PullRequestView.Mine:
                searchCriteria.CreatorId = connection.AuthorizedIdentity.Id;
                break;
            case PullRequestView.Assigned:
                searchCriteria.ReviewerId = connection.AuthorizedIdentity.Id;
                break;
            case PullRequestView.All:
                /* Nothing different for this */
                break;
        }

        // Get the pull requests with those criteria: (do we need internal id)
        var pullRequests = await gitClient.GetPullRequestsAsync(project.InternalId,  gitRepository.Id, searchCriteria, cancellationToken: cancellationToken);

        // Get the PullRequest PolicyClient. This client provides the State and Reason fields for each pull request
        using var policyClient = _azureClientProvider.GetClient<PolicyHttpClient>(azureUri.Connection, account);

        var repository = Repository.GetOrCreate(_dataStore, gitRepository, project.Id);

        var dsPullRequestSearch = PullRequestSearch.GetOrCreate(_dataStore, repository.Id, project.Id, account.Username, GetPullRequestView(pullRequestSearch.View));

        foreach (var pullRequest in pullRequests)
        {
            var status = PolicyStatus.Unknown;
            var statusReason = string.Empty;

            // ArtifactId is null in the pull request object and it is not the correct object. The ArtifactId for the
            // Policy Evaluations API is this:
            //     vstfs:///CodeReview/CodeReviewId/{projectId}/{pullRequestId}
            // Documentation: https://learn.microsoft.com/en-us/dotnet/api/microsoft.teamfoundation.policy.webapi.policyevaluationrecord.artifactid
            var artifactId = $"vstfs:///CodeReview/CodeReviewId/{project.InternalId}/{pullRequest.PullRequestId}";

            try
            {
                var policyEvaluations = await policyClient.GetPolicyEvaluationsAsync(project.InternalId, artifactId, cancellationToken: cancellationToken);
                GetPolicyStatus(policyEvaluations, out status, out statusReason);
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed getting policy evaluations for pull request: {pullRequest.PullRequestId} {pullRequest.Url}");
            }

            if (pullRequest.LastMergeSourceCommit is not null)
            {
                var commitRef = await gitClient.GetCommitAsync(pullRequest.LastMergeSourceCommit.CommitId, gitRepository.Id, cancellationToken: cancellationToken);
                if (commitRef is not null)
                {
                    pullRequest.LastMergeSourceCommit = commitRef;
                }
            }

            var creator = Identity.GetOrCreateIdentity(_dataStore, pullRequest.CreatedBy, connection);
            var dsPullRequest = PullRequest.GetOrCreate(_dataStore, pullRequest, repository.Id, creator.Id, statusReason);

            PullRequestSearchPullRequest.AddPullRequestToSearch(_dataStore, dsPullRequestSearch.Id, dsPullRequest.Id);
        }

        PullRequestSearchPullRequest.DeleteBefore(_dataStore, dsPullRequestSearch, DateTime.UtcNow - TimeSpan.FromMinutes(2));
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
            Log.Error($"Unknown Pull Request view for string: {viewStr}");
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
}
