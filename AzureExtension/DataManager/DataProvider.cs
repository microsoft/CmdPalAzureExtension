// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Account;
using AzureExtension.Client;
using AzureExtension.Controls;
using AzureExtension.DataModel;
using AzureExtension.Helpers;
using AzureExtension.PersistentData;
using Microsoft.Identity.Client;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.Policy.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.VisualStudio.Services.Identity;
using Microsoft.VisualStudio.Services.WebApi;
using Serilog;
using Windows.UI.Notifications;
using TFModels = Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace AzureExtension.DataManager;

public class DataProvider : IDataProvider
{
    private readonly ILogger _log;
    private readonly IAccountProvider _accountProvider;
    private readonly AzureClientProvider _azureClientProvider;

    public static readonly string IdentityRefFieldValueName = "Microsoft.VisualStudio.Services.WebApi.IdentityRef";
    public static readonly string SystemIdFieldName = "System.Id";
    public static readonly string WorkItemHtmlUrlFieldName = "DevHome.AzureExtension.WorkItemHtmlUrl";
    public static readonly string WorkItemTypeFieldName = "System.WorkItemType";

    public static readonly int PullRequestResultLimit = 25;

    public DataProvider(IAccountProvider accountProvider, AzureClientProvider azureClientProvider)
    {
        _log = Log.ForContext("SourceContext", nameof(IDataProvider));
        _accountProvider = accountProvider;
        _azureClientProvider = azureClientProvider;
    }

    public async Task<IEnumerable<WorkItem>> GetWorkItems(IQuery query)
    {
        var azureUri = new AzureUri(query.Url);
        var account = _accountProvider.GetDefaultAccount();
        var result = _azureClientProvider.GetVssConnection(azureUri.Connection, account);

        if (result.Result != ResultType.Success)
        {
            if (result.Exception != null)
            {
                throw result.Exception;
            }
            else
            {
                throw new AzureAuthorizationException($"Failed getting connection: {azureUri.Connection} with {result.Error}");
            }
        }

        var witClient = result.Connection!.GetClient<WorkItemTrackingHttpClient>();
        if (witClient == null)
        {
            throw new AzureClientException($"Failed getting WorkItemTrackingHttpClient");
        }

        // Good practice to only create data after we know the client is valid, but any exceptions
        // will roll back the transaction.
        var teamProject = GetTeamProject(azureUri.Project, account, azureUri.Connection);

        var getQueryResult = await witClient.GetQueryAsync(teamProject.Id, azureUri.Query);
        if (getQueryResult == null)
        {
            throw new AzureClientException($"GetQueryAsync failed for {azureUri.Connection}, {teamProject.Id}, {azureUri.Query}");
        }

        var queryId = new Guid(azureUri.Query);
        var queryResult = await witClient.QueryByIdAsync(teamProject.Id, queryId);

        if (queryResult == null)
        {
            throw new AzureClientException($"QueryByIdAsync failed for {azureUri.Connection}, {teamProject.Id}, {queryId}");
        }

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

                        // Query result limit
                        if (workItemIds.Count >= 25)
                        {
                            break;
                        }
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
                        if (workItemIds.Count >= 25)
                        {
                            break;
                        }
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
            workItems = await witClient.GetWorkItemsAsync(teamProject.Id, workItemIds, null, null, TFModels.WorkItemExpand.Links, TFModels.WorkItemErrorPolicy.Omit);
            if (workItems == null)
            {
                throw new AzureClientException($"GetWorkItemsAsync failed for {azureUri.Connection}, {teamProject.Id}, Ids: {string.Join(",", workItemIds.ToArray())}");
            }
        }

        var workItemsList = new List<WorkItem>();

        foreach (var workItem in workItems)
        {
            var cmdPalWorkItem = new WorkItem();

            cmdPalWorkItem.AddSystemId(workItem.Id);

            // cmdPalWorkItem.Icon = new IconInfo(GetIconForType(workItem.Fields[AzureDataManager.WorkItemTypeFieldName]?.ToString()));
            // cmdPalWorkItem.StatusIcon = new IconInfo(GetIconForStatusState(workItem.Fields["System.State"]?.ToString()));
            var htmlUrl = Links.GetLinkHref(workItem.Links, "html");
            cmdPalWorkItem.AddHtmlUrl(htmlUrl);

            var requestOptions = RequestOptions.RequestOptionsDefault();

            foreach (var field in requestOptions.Fields)
            {
                if (!workItem.Fields.ContainsKey(field))
                {
                    continue;
                }

                var fieldValue = workItem.Fields[field].ToString();
                if (fieldValue is null)
                {
                    continue;
                }

                if (workItem.Fields[field] is DateTime dateTime)
                {
                    if (field == "System.CreatedDate")
                    {
                        cmdPalWorkItem.SystemCreatedDate = dateTime.Ticks;
                    }
                    else if (field == "System.ChangedDate")
                    {
                        cmdPalWorkItem.SystemChangedDate = dateTime.Ticks;
                    }

                    continue;
                }

                var fieldIdentityRef = workItem.Fields[field] as IdentityRef;
                if (fieldValue == IdentityRefFieldValueName && fieldIdentityRef != null)
                {
                    var identity = DataModel.Identity.CreateFromIdentityRef(fieldIdentityRef, result.Connection);

                    if (field == "System.CreatedBy")
                    {
                        cmdPalWorkItem.SystemCreatedBy = identity;
                    }
                    else if (field == "System.ChangedBy")
                    {
                        cmdPalWorkItem.SystemChangedBy = identity;
                    }

                    continue;
                }

                if (field == WorkItemTypeFieldName)
                {
                    // Need a separate query to create WorkItemType object.
                    var workItemTypeInfo = await witClient!.GetWorkItemTypeAsync(teamProject.Id, fieldValue);
                    var workItemType = WorkItemType.CreateFromTeamWorkItemType(workItemTypeInfo, 1);

                    cmdPalWorkItem.SystemWorkItemType = workItemType;
                    continue;
                }

                if (field == "System.State")
                {
                    cmdPalWorkItem.SystemState = fieldValue;
                    continue;
                }

                if (field == "System.Reason")
                {
                    cmdPalWorkItem.SystemReason = fieldValue;
                    continue;
                }

                if (field == "System.Title")
                {
                    cmdPalWorkItem.SystemTitle = fieldValue;
                    continue;
                }
            }

            workItemsList.Add(cmdPalWorkItem);
        }

        return workItemsList;
    }

    private TeamProject GetTeamProject(string projectName, IAccount account, Uri connection)
    {
        var result = _azureClientProvider.GetVssConnection(connection, account);

        if (result.Result != ResultType.Success)
        {
            if (result.Exception != null)
            {
                throw result.Exception;
            }
            else
            {
                throw new AzureAuthorizationException($"Failed getting connection: {connection} for {account.Username} with {result.Error}");
            }
        }

        var projectClient = new ProjectHttpClient(result.Connection!.Uri, result.Connection!.Credentials);
        if (projectClient == null)
        {
            throw new AzureClientException($"Failed getting ProjectHttpClient for {connection}");
        }

        var project = projectClient.GetProject(projectName).Result;
        if (project == null)
        {
            throw new AzureClientException($"Project reference was null for {connection} and Project: {projectName}");
        }

        return project;
    }

    public async Task<IEnumerable<PullRequest>> GetPullRequests(PullRequestSearch pullRequestSearch)
    {
        var azureUri = new AzureUri(pullRequestSearch.Url);
        var account = _accountProvider.GetDefaultAccount();
        var connectionResult = _azureClientProvider.GetVssConnection(azureUri.Connection, account);

        if (connectionResult.Result != ResultType.Success)
        {
            if (connectionResult.Exception != null)
            {
                throw connectionResult.Exception;
            }
            else
            {
                throw new AzureAuthorizationException($"Failed getting connection: {azureUri.Connection} with {connectionResult.Error}");
            }
        }

        var gitClient = connectionResult.Connection!.GetClient<GitHttpClient>();
        if (gitClient == null)
        {
            throw new AzureClientException($"Failed getting GitHttpClient");
        }

        var teamProject = GetTeamProject(azureUri.Project, account, azureUri.Connection);

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
                searchCriteria.CreatorId = connectionResult.Connection!.AuthorizedIdentity.Id;
                break;
            case PullRequestView.Assigned:
                searchCriteria.ReviewerId = connectionResult.Connection!.AuthorizedIdentity.Id;
                break;
            case PullRequestView.All:
                /* Nothing different for this */
                break;
        }

        // Get the pull requests with those criteria: (do we need internal id)
        var pullRequests = await gitClient.GetPullRequestsAsync(teamProject.Id, gitRepository.Id, searchCriteria, null, null, PullRequestResultLimit);

        // Get the PullRequest PolicyClient. This client provides the State and Reason fields for each pull request
        var policyClient = connectionResult.Connection!.GetClient<PolicyHttpClient>();

        var pullRequestList = new List<PullRequest>();
        foreach (var pullRequest in pullRequests)
        {
            var status = PolicyStatus.Unknown;
            var statusReason = string.Empty;

            // ArtifactId is null in the pull request object and it is not the correct object. The ArtifactId for the
            // Policy Evaluations API is this:
            //     vstfs:///CodeReview/CodeReviewId/{projectId}/{pullRequestId}
            // Documentation: https://learn.microsoft.com/en-us/dotnet/api/microsoft.teamfoundation.policy.webapi.policyevaluationrecord.artifactid
            var artifactId = $"vstfs:///CodeReview/CodeReviewId/{teamProject.Id}/{pullRequest.PullRequestId}";

            // Url in the GitPullRequest object is a REST Api Url, and the links lack an html Url, so we must build it.
            var htmlUrl = $"{gitRepository.WebUrl}/pullrequest/{pullRequest.PullRequestId}";

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
                if (gitClient is not null)
                {
                    var commitRef = await gitClient.GetCommitAsync(pullRequest.LastMergeSourceCommit.CommitId, gitRepository.Id);
                    if (commitRef is not null)
                    {
                        pullRequest.LastMergeSourceCommit = commitRef;
                    }
                }
            }

            var pullRequestObject = new PullRequest();
            pullRequestObject.Id = pullRequest.PullRequestId;
            pullRequestObject.Title = pullRequest.Title;
            pullRequestObject.Status = pullRequest.Status.ToString();
            pullRequestObject.PolicyStatus = status.ToString();
            pullRequestObject.PolicyStatusReason = statusReason;
            pullRequestObject.TargetBranch = pullRequest.TargetRefName;
            pullRequestObject.CreationDate = pullRequest.CreationDate.Ticks;
            pullRequestObject.HtmlUrl = htmlUrl;
            pullRequestObject.RepositoryGuid = gitRepository.Id;

            var creator = DataModel.Identity.CreateFromIdentityRef(pullRequest.CreatedBy, connectionResult.Connection);
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
}
