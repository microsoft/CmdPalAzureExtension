// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Account;
using AzureExtension.Client;
using AzureExtension.Controls;
using AzureExtension.Data;
using AzureExtension.DataModel;
using AzureExtension.Helpers;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.Policy.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Serilog;
using Query = AzureExtension.DataModel.Query;
using TFModels = Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using WorkItem = AzureExtension.DataModel.WorkItem;

namespace AzureExtension.DataManager;

public class AzureDataManager : IDataUpdateService, IDataObjectProvider
{
    private readonly ILogger _log;
    private readonly DataStore _dataStore;
    private readonly IAccountProvider _accountProvider;
    private readonly AzureClientProvider _azureClientProvider;

    public AzureDataManager(
        DataStore dataStore,
        IAccountProvider accountProvider,
        AzureClientProvider azureClientProvider)
    {
        _log = Log.ForContext("SourceContext", nameof(IDataProvider));
        _dataStore = dataStore;
        _accountProvider = accountProvider;
        _azureClientProvider = azureClientProvider;
    }

    private void ValidateDataStore()
    {
        if (_dataStore == null || !_dataStore.IsConnected)
        {
            throw new DataStoreInaccessibleException("Cache DataStore is not available.");
        }
    }

    private const string LastUpdatedKeyName = "LastUpdated";

    public event DataManagerUpdateEventHandler? OnUpdate;

    public DateTime LastUpdated
    {
        get
        {
            ValidateDataStore();
            var lastUpdated = MetaData.Get(_dataStore, LastUpdatedKeyName);
            if (lastUpdated == null)
            {
                return DateTime.MinValue;
            }

            return lastUpdated.ToDateTime();
        }

        set
        {
            ValidateDataStore();
            MetaData.AddOrUpdate(_dataStore, LastUpdatedKeyName, value.ToDataStoreString());
        }
    }

    public Query? GetQuery(IQuery query)
    {
        ValidateDataStore();
        var account = _accountProvider.GetDefaultAccount();
        var azureUri = new AzureUri(query.Url);
        return Query.Get(_dataStore, azureUri.Query, account.Username);
    }

    public IEnumerable<IWorkItem> GetWorkItems(IQuery query)
    {
        ValidateDataStore();
        var dsQuery = GetQuery(query);
        return WorkItem.GetForQuery(_dataStore, dsQuery!);
    }

    private async Task UpdateQueryAsync(IQuery query, CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew(); // Start measuring time

        var azureUri = new AzureUri(query.Url);
        var account = _accountProvider.GetDefaultAccount();
        var connection = _azureClientProvider.GetVssConnection(azureUri.Connection, account);

        var witClient = connection.GetClient<WorkItemTrackingHttpClient>();
        if (witClient == null)
        {
            throw new AzureClientException($"Failed getting WorkItemTrackingHttpClient");
        }

        // Good practice to only create data after we know the client is valid, but any exceptions
        // will roll back the transaction.
        var org = Organization.GetOrCreate(_dataStore, azureUri.Connection);

        var project = Project.Get(_dataStore, azureUri.Project, org.Id);
        if (project is null)
        {
            var projectClient = new ProjectHttpClient(connection.Uri, connection.Credentials);
            var teamProject = await projectClient.GetProject(azureUri.Project);
            project = Project.GetOrCreateByTeamProject(_dataStore, teamProject, org.Id);
        }

        var queryId = new Guid(azureUri.Query);
        var queryResult = await witClient.QueryByIdAsync(project.InternalId, queryId, cancellationToken: cancellationToken);

        var workItemIds = new List<int>();

        // The WorkItems collection and individual reference objects may be null.
        switch (queryResult.QueryType)
        {
            // Tree types are treated as flat, but the data structure is different.
            case TFModels.QueryType.Tree:
                if (queryResult.WorkItemRelations is not null)
                {
                    foreach (var workItemRelation in queryResult.WorkItemRelations)
                    {
                        if (workItemRelation is null || workItemRelation.Target is null)
                        {
                            continue;
                        }

                        workItemIds.Add(workItemRelation.Target.Id);
                    }
                }

                break;

            case TFModels.QueryType.Flat:
                if (queryResult.WorkItems is not null)
                {
                    foreach (var item in queryResult.WorkItems)
                    {
                        if (item is null)
                        {
                            continue;
                        }

                        workItemIds.Add(item.Id);
                    }
                }

                break;

            case TFModels.QueryType.OneHop:

                // OneHop work item structure is the same as the tree type.
                goto case TFModels.QueryType.Tree;

            default:
                break;
        }

        var workItems = new List<TFModels.WorkItem>();
        if (workItemIds.Count > 0)
        {
            workItems = await witClient.GetWorkItemsAsync(project.InternalId, workItemIds, null, null, TFModels.WorkItemExpand.Links, TFModels.WorkItemErrorPolicy.Omit, cancellationToken: cancellationToken);
        }

        var workItemsList = new List<WorkItem>();
        var dsQuery = Query.GetOrCreate(_dataStore, azureUri.Query, project.Id, account.Username, query.Name);

        foreach (var workItem in workItems)
        {
            var fieldValue = workItem.Fields["System.WorkItemType"].ToString();
            var workItemTypeInfo = await witClient.GetWorkItemTypeAsync(project.InternalId, fieldValue, cancellationToken: cancellationToken);
            var cmdPalWorkItem = WorkItem.GetOrCreate(_dataStore, workItem, connection, project.Id, workItemTypeInfo);
            QueryWorkItem.AddWorkItemToQuery(_dataStore, dsQuery.Id, cmdPalWorkItem.Id);
            workItemsList.Add(cmdPalWorkItem);
        }

        stopwatch.Stop(); // Stop measuring time
        _log.Information($"UpdateWorkItems took {stopwatch.ElapsedMilliseconds} ms to complete.");
    }

    public async Task<IEnumerable<IPullRequest>> UpdatePullRequestsAsync(IPullRequestSearch pullRequestSearch)
    {
        var azureUri = new AzureUri(pullRequestSearch.Url);
        var account = _accountProvider.GetDefaultAccount();
        var connection = _azureClientProvider.GetVssConnection(azureUri.Connection, account);

        var gitClient = connection.GetClient<GitHttpClient>();
        if (gitClient == null)
        {
            throw new AzureClientException($"Failed getting GitHttpClient");
        }

        var projectClient = new ProjectHttpClient(connection.Uri, connection.Credentials);
        var teamProject = await projectClient.GetProject(azureUri.Project);

        var gitRepository = await gitClient.GetRepositoryAsync(teamProject.Id, azureUri.Repository);

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
        var pullRequests = await gitClient.GetPullRequestsAsync(teamProject.Id, gitRepository.Id, searchCriteria);

        // Get the PullRequest PolicyClient. This client provides the State and Reason fields for each pull request
        var policyClient = connection.GetClient<PolicyHttpClient>();

        var pullRequestList = new List<IPullRequest>();
        foreach (var pullRequest in pullRequests)
        {
            var status = PolicyStatus.Unknown;
            var statusReason = string.Empty;

            // ArtifactId is null in the pull request object and it is not the correct object. The ArtifactId for the
            // Policy Evaluations API is this:
            //     vstfs:///CodeReview/CodeReviewId/{projectId}/{pullRequestId}
            // Documentation: https://learn.microsoft.com/en-us/dotnet/api/microsoft.teamfoundation.policy.webapi.policyevaluationrecord.artifactid
            var artifactId = $"vstfs:///CodeReview/CodeReviewId/{teamProject.Id}/{pullRequest.PullRequestId}";

            try
            {
                var policyEvaluations = await policyClient.GetPolicyEvaluationsAsync(teamProject.Id, artifactId);
                GetPolicyStatus(policyEvaluations, out status, out statusReason);
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed getting policy evaluations for pull request: {pullRequest.PullRequestId} {pullRequest.Url}");
            }

            if (pullRequest.LastMergeSourceCommit is not null)
            {
                var commitRef = await gitClient.GetCommitAsync(pullRequest.LastMergeSourceCommit.CommitId, gitRepository.Id);
                if (commitRef is not null)
                {
                    pullRequest.LastMergeSourceCommit = commitRef;
                }
            }

            var pullRequestObject = new IPullRequest();
            pullRequestObject.Id = pullRequest.PullRequestId;
            pullRequestObject.Title = pullRequest.Title;
            pullRequestObject.Status = pullRequest.Status.ToString();
            pullRequestObject.PolicyStatus = status.ToString();
            pullRequestObject.PolicyStatusReason = statusReason;
            pullRequestObject.TargetBranch = pullRequest.TargetRefName;
            pullRequestObject.CreationDate = pullRequest.CreationDate.Ticks;
            pullRequestObject.HtmlUrl = htmlUrl;
            pullRequestObject.RepositoryGuid = gitRepository.Id;

            var creator = DataModel.Identity.CreateFromIdentityRef(pullRequest.CreatedBy, connection);
            pullRequestObject.Creator = creator;

            pullRequestList.Add(pullRequestObject);
        }

        return pullRequestList;
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

    private static bool IsCancelException(Exception ex)
    {
        return (ex is OperationCanceledException) || (ex is TaskCanceledException);
    }

    private async Task PerformUpdateAsync(DataUpdateParameters parameters, Func<Task> asyncOperation)
    {
        using var tx = _dataStore.Connection!.BeginTransaction();

        try
        {
            await asyncOperation();

            // PruneObsoleteData();
            // SetLastUpdatedInMetaData();
        }
        catch (Exception ex) when (IsCancelException(ex))
        {
            tx.Rollback();
            OnUpdate?.Invoke(this, new DataManagerUpdateEventArgs(DataManagerUpdateKind.Cancel, parameters, ex));
            _log.Information($"Update cancelled: {parameters}");
            return;
        }
        catch (Exception ex)
        {
            tx.Rollback();
            _log.Error(ex, $"Error during update: {ex.Message}");
            return;
        }

        tx.Commit();
        _log.Information($"Update complete: {parameters}");
    }

    public async Task UpdateData(DataUpdateParameters parameters)
    {
        var type = parameters.UpdateType;

        Func<Task> operation = type switch
        {
            DataUpdateType.Query => async () => await UpdateQueryAsync((parameters.UpdateObject as IQuery)!, parameters.CancellationToken.GetValueOrDefault()),
            _ => throw new NotImplementedException($"Update type {type} not implemented."),
        };

        await PerformUpdateAsync(parameters, operation);

        OnUpdate?.Invoke(this, new DataManagerUpdateEventArgs(DataManagerUpdateKind.Success, parameters));
    }
}
