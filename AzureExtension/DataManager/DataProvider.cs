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

    private readonly IDataObjectProvider _dataObjectProvider;

    public static readonly string IdentityRefFieldValueName = "Microsoft.VisualStudio.Services.WebApi.IdentityRef";
    public static readonly string SystemIdFieldName = "System.Id";
    public static readonly string WorkItemHtmlUrlFieldName = "DevHome.AzureExtension.WorkItemHtmlUrl";
    public static readonly string WorkItemTypeFieldName = "System.WorkItemType";

    public static readonly int PullRequestResultLimit = 25;

        public DataProvider(IDataObjectProvider dataObjectProvider, ICacheManager cacheManager)
    {
        _log = Log.ForContext("SourceContext", nameof(IDataProvider));
        _cacheManager = cacheManager;
        _dataObjectProvider = dataObjectProvider;

        _cacheManager.OnUpdate += OnCacheManagerUpdate;
    }

    public async Task<IEnumerable<IWorkItem>> GetWorkItems(IQuery query)
    {
        var dsQuery = _dataObjectProvider.GetQuery(query);
        if (dsQuery == null)
        {
            var parameters = new DataUpdateParameters
            {
                UpdateType = DataUpdateType.Query,
                UpdateObject = query,
            };
            await _cacheManager.RequestRefresh(parameters);
        }

        return _dataObjectProvider.GetWorkItems(query);
    }

    public void OnCacheManagerUpdate(object? source, CacheManagerUpdateEventArgs e)
    {
        OnUpdate?.Invoke(source, e);
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
