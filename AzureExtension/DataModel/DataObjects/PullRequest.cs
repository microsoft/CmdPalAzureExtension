// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls;
using AzureExtension.Data;
using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.CommandPalette.Extensions;
using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace AzureExtension.DataModel;

[Table("PullRequest")]
public class PullRequest : IPullRequest
{
    [Key]
    public long Id { get; set; } = DataStore.NoForeignKey;

    public long InternalId { get; set; } = DataStore.NoForeignKey;

    public string Name { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public long RepositoryId { get; set; } = DataStore.NoForeignKey;

    public long CreatorId { get; set; } = DataStore.NoForeignKey;

    public string Status { get; set; } = string.Empty;

    public string PolicyStatus { get; set; } = string.Empty;

    public string PolicyStatusReason { get; set; } = string.Empty;

    public string TargetBranch { get; set; } = string.Empty;

    public long CreationDate { get; set; } = DataStore.NoForeignKey;

    public string HtmlUrl { get; set; } = string.Empty;

    [Write(false)]
    private DataStore? DataStore { get; set; }

    [Write(false)]
    public string RepositoryGuid => Repository.Get(DataStore, RepositoryId).InternalId;

    [Write(false)]
    public Identity? Creator => Identity.Get(DataStore, CreatorId);

    public bool IsTopLevel => throw new NotImplementedException();

    private static PullRequest Create(
        DataStore dataStore,
        GitPullRequest gitPullRequest,
        long repositoryId,
        long creatorId,
        string statusReason)
    {
        var pullRequest = new PullRequest
        {
            InternalId = gitPullRequest.PullRequestId,
            RepositoryId = repositoryId,
            CreatorId = creatorId,
            Name = gitPullRequest.Title,
            Url = gitPullRequest.Url,
            Status = gitPullRequest.Status.ToString(),
            PolicyStatus = statusReason,
            TargetBranch = gitPullRequest.TargetRefName,
            CreationDate = gitPullRequest.CreationDate.Ticks,
        };

        var repository = Repository.Get(dataStore, repositoryId);

        // Url in the GitPullRequest object is a REST Api Url, and the links lack an html Url, so we must build it.
        pullRequest.HtmlUrl = $"{repository.CloneUrl}/pullrequest/{gitPullRequest.PullRequestId}";

        pullRequest.Id = dataStore.Connection.Insert(pullRequest);
        return pullRequest;
    }

    private static PullRequest? GetByInternalId(DataStore dataStore, long internalId)
    {
        var sql = @"SELECT * FROM PullRequest WHERE InternalId = @InternalId";
        var param = new
        {
            InternalId = internalId,
        };

        var pullRequest = dataStore.Connection!.QueryFirstOrDefault<PullRequest>(sql, param, null);

        if (pullRequest != null)
        {
            pullRequest.DataStore = dataStore;
        }

        return pullRequest;
    }

    public static PullRequest AddOrUpdate(DataStore dataStore, PullRequest pullRequest)
    {
        var existingPullRequest = GetByInternalId(dataStore, pullRequest.InternalId);
        if (existingPullRequest != null)
        {
            pullRequest.Id = existingPullRequest.Id;
            dataStore.Connection.Update(pullRequest);
            pullRequest.DataStore = dataStore;
            return pullRequest;
        }

        pullRequest.DataStore = dataStore;
        pullRequest.Id = dataStore.Connection.Insert(pullRequest);
        return pullRequest;
    }

    public static PullRequest GetOrCreate(DataStore dataStore, GitPullRequest gitPullRequest, long repositoryId, long creatorId, string statusReason)
    {
        var pullRequest = Create(dataStore, gitPullRequest, repositoryId, creatorId, statusReason);
        return AddOrUpdate(dataStore, pullRequest);
    }

    public static IEnumerable<PullRequest> GetForPullRequestSearch(DataStore dataStore, PullRequestSearch pullRequestSearch)
    {
        var sql = @"SELECT * FROM PullRequest WHERE Id IN (SELECT PullRequest FROM PullRequestSearchPullRequest WHERE PullRequestSearch = @PullRequestSearchId ORDER BY TimeUpdated ASC)";
        var param = new
        {
            PullRequestSearchId = pullRequestSearch.Id,
        };

        var pullRequests = dataStore.Connection!.Query<PullRequest>(sql, param, null);
        foreach (var pullRequest in pullRequests)
        {
            pullRequest.DataStore = dataStore;
        }

        return pullRequests;
    }

    public static void DeleteNotReferencedBySearch(DataStore dataStore)
    {
        var sql = @"DELETE FROM PullRequest WHERE Id NOT IN (SELECT PullRequest FROM PullRequestSearchPullRequest)";
        var rowsDeleted = dataStore.Connection!.Execute(sql);
    }
}
