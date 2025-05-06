// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.Policy.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Profile;
using Microsoft.VisualStudio.Services.WebApi;

namespace AzureExtension.Client;

public interface IAzureLiveDataProvider
{
    Task<Avatar> GetAvatarAsync(IVssConnection connection, Guid identity);

    Task<TeamProject> GetTeamProject(IVssConnection connection, string id);

    Task<GitRepository> GetRepositoryAsync(IVssConnection connection, string projectId, string repositoryId, CancellationToken cancellationToken);

    Task<WorkItemQueryResult> GetWorkItemQueryResultByIdAsync(IVssConnection connection, string projectId, Guid queryId, CancellationToken cancellationToken);

    Task<List<WorkItem>> GetWorkItemsAsync(IVssConnection connection, string projectId, List<int> workItemIds, WorkItemExpand expand, WorkItemErrorPolicy errorPolicy, CancellationToken cancellationToken);

    Task<WorkItemType> GetWorkItemTypeAsync(IVssConnection connection, string projectId, string? fieldValue, CancellationToken cancellationToken);

    Task<List<GitPullRequest>> GetPullRequestsAsync(IVssConnection connection, string projectId, Guid repositoryId, GitPullRequestSearchCriteria searchCriteria, CancellationToken cancellationToken);

    Task<List<PolicyEvaluationRecord>> GetPolicyEvaluationsAsync(IVssConnection connection, string projectId, string artifactId, CancellationToken cancellationToken);

    Task<GitCommit> GetCommitAsync(IVssConnection connection, string commitId, Guid repositoryId, CancellationToken cancellationToken);

    Task<List<Build>> GetBuildsAsync(IVssConnection connection, string projectId, long definitionId, CancellationToken cancellationToken);
}
