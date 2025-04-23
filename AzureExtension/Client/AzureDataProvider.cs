// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Account;
using AzureExtension.DataManager;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.Policy.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Profile;
using Microsoft.VisualStudio.Services.Profile.Client;

namespace AzureExtension.Client;

public class AzureDataProvider : IAzureLiveDataProvider
{
    private readonly AzureClientProvider _clientProvider;
    private readonly IAccountProvider _accountProvider;

    public AzureDataProvider(AzureClientProvider azureClientProvider, IAccountProvider accountProvider)
    {
        _clientProvider = azureClientProvider;
        _accountProvider = accountProvider;
    }

    public async Task<Avatar> GetAvatarAsync(Uri connection, Guid identity)
    {
        var account = _accountProvider.GetDefaultAccount();
        using var client = _clientProvider.GetClient<ProfileHttpClient>(connection, account);
        return await client.GetAvatarAsync(identity, AvatarSize.Small);
    }

    public async Task<GitCommit> GetCommitAsync(Uri connection, string commitId, Guid repositoryId, CancellationToken cancellationToken)
    {
        var account = _accountProvider.GetDefaultAccount();
        using var gitClient = _clientProvider.GetClient<GitHttpClient>(connection, account);
        return await gitClient.GetCommitAsync(commitId, repositoryId, cancellationToken: cancellationToken);
    }

    public async Task<List<PolicyEvaluationRecord>> GetPolicyEvaluationsAsync(Uri connection, string projectId, string artifactId, CancellationToken cancellationToken)
    {
        var account = _accountProvider.GetDefaultAccount();

        // Get the PullRequest PolicyClient. This client provides the State and Reason fields for each pull request
        using var policyClient = _clientProvider.GetClient<PolicyHttpClient>(connection, account);
        return await policyClient.GetPolicyEvaluationsAsync(projectId, artifactId, cancellationToken: cancellationToken);
    }

    public async Task<List<GitPullRequest>> GetPullRequestsAsync(Uri connection, string projectId, Guid repositoryId, GitPullRequestSearchCriteria searchCriteria, CancellationToken cancellationToken)
    {
        var account = _accountProvider.GetDefaultAccount();
        using var gitClient = _clientProvider.GetClient<GitHttpClient>(connection, account);
        return await gitClient.GetPullRequestsAsync(projectId, repositoryId, searchCriteria, cancellationToken: cancellationToken);
    }

    public async Task<GitRepository> GetRepositoryAsync(Uri connection, string projectId, string repositoryId, CancellationToken cancellationToken)
    {
        var account = _accountProvider.GetDefaultAccount();
        using var gitClient = _clientProvider.GetClient<GitHttpClient>(connection, account);
        return await gitClient.GetRepositoryAsync(projectId, repositoryId, cancellationToken: cancellationToken);
    }

    public async Task<TeamProject> GetTeamProject(Uri connection, string id)
    {
        var account = _accountProvider.GetDefaultAccount();
        using var projectClient = _clientProvider.GetClient<ProjectHttpClient>(connection, account);
        return await projectClient.GetProject(id);
    }

    public async Task<WorkItemQueryResult> GetWorkItemQueryResultByIdAsync(Uri connection, string projectId, Guid queryId, CancellationToken cancellationToken)
    {
        var account = _accountProvider.GetDefaultAccount();
        using var witClient = _clientProvider.GetClient<WorkItemTrackingHttpClient>(connection, account);
        return await witClient.QueryByIdAsync(projectId, queryId, cancellationToken: cancellationToken);
    }

    public async Task<List<WorkItem>> GetWorkItemsAsync(Uri connection, string projectId, List<int> workItemIds, WorkItemExpand expand, WorkItemErrorPolicy errorPolicy, CancellationToken cancellationToken)
    {
        var account = _accountProvider.GetDefaultAccount();
        using var witClient = _clientProvider.GetClient<WorkItemTrackingHttpClient>(connection, account);
        return await witClient.GetWorkItemsAsync(projectId, workItemIds, null, null, expand, errorPolicy, cancellationToken: cancellationToken);
    }

    public async Task<WorkItemType> GetWorkItemTypeAsync(Uri connection, string projectId, string? fieldValue, CancellationToken cancellationToken)
    {
        var account = _accountProvider.GetDefaultAccount();
        using var witClient = _clientProvider.GetClient<WorkItemTrackingHttpClient>(connection, account);
        return await witClient.GetWorkItemTypeAsync(projectId, fieldValue, cancellationToken: cancellationToken);
    }
}
