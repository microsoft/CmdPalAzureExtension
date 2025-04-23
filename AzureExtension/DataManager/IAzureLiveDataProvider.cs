// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.Policy.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace AzureExtension.DataManager;

public interface IAzureLiveDataProvider
{
    Task<TeamProject> GetTeamProject(Uri connection, string id);

    Task<GitRepository> GetRepositoryAsync(Uri connection, string projectId, string repositoryId, CancellationToken cancellationToken);

    Task<WorkItemQueryResult> GetWorkItemQueryResultByIdAsync(Uri connection, string projectId, Guid queryId, CancellationToken cancellationToken);

    Task<List<WorkItem>> GetWorkItemsAsync(Uri connection, string projectId, List<int> workItemIds, WorkItemExpand links, WorkItemErrorPolicy omit, CancellationToken cancellationToken);

    Task<WorkItemType> GetWorkItemTypeAsync(Uri connection, string projectId, string? fieldValue, CancellationToken cancellationToken);

    Task<List<GitPullRequest>> GetPullRequestsAsync(Uri connection, string projectId, Guid repositoryId, GitPullRequestSearchCriteria searchCriteria, CancellationToken cancellationToken);

    Task<List<PolicyEvaluationRecord>> GetPolicyEvaluationsAsync(Uri connection, string projectId, string artifactId, CancellationToken cancellationToken);

    Task<GitCommit> GetCommitAsync(Uri connection, string commitId, Guid repositoryId, CancellationToken cancellationToken);
}
