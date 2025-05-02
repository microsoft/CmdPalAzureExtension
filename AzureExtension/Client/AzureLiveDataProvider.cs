// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.Policy.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Profile;
using Microsoft.VisualStudio.Services.Profile.Client;
using Microsoft.VisualStudio.Services.WebApi;
using Serilog;

namespace AzureExtension.Client;

public class AzureLiveDataProvider : IAzureLiveDataProvider
{
    private readonly ILogger _log;

    public AzureLiveDataProvider()
    {
        _log = Log.ForContext("SourceContext", nameof(AzureLiveDataProvider));
    }

    public async Task<Avatar> GetAvatarAsync(IVssConnection connection, Guid identity)
    {
        var client = connection.GetClient<ProfileHttpClient>();
        return await client.GetAvatarAsync(identity, AvatarSize.Small);
    }

    public async Task<GitCommit> GetCommitAsync(IVssConnection connection, string commitId, Guid repositoryId, CancellationToken cancellationToken)
    {
        var gitClient = connection.GetClient<GitHttpClient>();
        return await gitClient.GetCommitAsync(commitId, repositoryId, cancellationToken: cancellationToken);
    }

    public async Task<List<PolicyEvaluationRecord>> GetPolicyEvaluationsAsync(IVssConnection connection, string projectId, string artifactId, CancellationToken cancellationToken)
    {
        // Get the PullRequest PolicyClient. This client provides the State and Reason fields for each pull request
        var policyClient = connection.GetClient<PolicyHttpClient>();

        // _log.Information($"Got PolicyClient for {artifactId}.");
        return await policyClient.GetPolicyEvaluationsAsync(projectId, artifactId, cancellationToken: cancellationToken);
    }

    public async Task<List<GitPullRequest>> GetPullRequestsAsync(IVssConnection connection, string projectId, Guid repositoryId, GitPullRequestSearchCriteria searchCriteria, CancellationToken cancellationToken)
    {
        var gitClient = connection.GetClient<GitHttpClient>();
        return await gitClient.GetPullRequestsAsync(projectId, repositoryId, searchCriteria, cancellationToken: cancellationToken);
    }

    public async Task<GitRepository> GetRepositoryAsync(IVssConnection connection, string projectId, string repositoryId, CancellationToken cancellationToken)
    {
        var gitClient = connection.GetClient<GitHttpClient>();
        return await gitClient.GetRepositoryAsync(projectId, repositoryId, cancellationToken: cancellationToken);
    }

    public async Task<TeamProject> GetTeamProject(IVssConnection connection, string id)
    {
        var projectClient = connection.GetClient<ProjectHttpClient>();
        return await projectClient.GetProject(id);
    }

    public async Task<WorkItemQueryResult> GetWorkItemQueryResultByIdAsync(IVssConnection connection, string projectId, Guid queryId, CancellationToken cancellationToken)
    {
        var witClient = connection.GetClient<WorkItemTrackingHttpClient>();
        return await witClient.QueryByIdAsync(projectId, queryId, cancellationToken: cancellationToken);
    }

    public async Task<List<WorkItem>> GetWorkItemsAsync(IVssConnection connection, string projectId, List<int> workItemIds, WorkItemExpand expand, WorkItemErrorPolicy errorPolicy, CancellationToken cancellationToken)
    {
        var witClient = connection.GetClient<WorkItemTrackingHttpClient>();
        return await witClient.GetWorkItemsAsync(projectId, workItemIds, null, null, expand, errorPolicy, cancellationToken: cancellationToken);
    }

    public async Task<WorkItemType> GetWorkItemTypeAsync(IVssConnection connection, string projectId, string? fieldValue, CancellationToken cancellationToken)
    {
        var witClient = connection.GetClient<WorkItemTrackingHttpClient>();
        return await witClient.GetWorkItemTypeAsync(projectId, fieldValue, cancellationToken: cancellationToken);
    }
}
