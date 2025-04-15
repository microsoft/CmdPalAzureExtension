// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Data;
using AzureExtension.Helpers;
using Dapper;
using Dapper.Contrib.Extensions;

namespace AzureExtension.DataModel.DataObjects;

[Table("PullRequestSearchPullRequest")]
public class PullRequestSearchPullRequest
{
    [Key]
    public long Id { get; set; } = DataStore.NoForeignKey;

    public long TimeUpdated { get; set; } = DataStore.NoForeignKey;

    public long PullRequestSearch { get; set; } = DataStore.NoForeignKey;

    public long PullRequest { get; set; } = DataStore.NoForeignKey;

    [Write(false)]
    [Computed]
    public DateTime UpdatedAt => TimeUpdated.ToDateTime();

    private static PullRequestSearchPullRequest GetByPullRequestSearchIdAndPullRequestId(
        DataStore dataStore,
        long pullRequestSearchId,
        long pullRequestId)
    {
        var sql = "SELECT * FROM PullRequestSearchPullRequest WHERE PullRequestSearch = @PullRequestSearchId AND PullRequest = @PullRequestId";
        var param = new
        {
            PullRequestSearchId = pullRequestSearchId,
            PullRequestId = pullRequestId,
        };
        var pullRequestSearchPullRequest = dataStore.Connection.QueryFirstOrDefault<PullRequestSearchPullRequest>(sql, param, null);
        return pullRequestSearchPullRequest;
    }

    public static PullRequestSearchPullRequest AddPullRequestToSearch(DataStore dataStore, long pullRequestSearchId, long pullRequestId)
    {
        var existingPullRequestSearchPullRequest = GetByPullRequestSearchIdAndPullRequestId(dataStore, pullRequestSearchId, pullRequestId);

        if (existingPullRequestSearchPullRequest != null)
        {
            return existingPullRequestSearchPullRequest;
        }

        var newPullRequestSearchPullRequest = new PullRequestSearchPullRequest
        {
            PullRequestSearch = pullRequestSearchId,
            PullRequest = pullRequestId,
            TimeUpdated = DateTime.UtcNow.ToDataStoreInteger(),
        };

        newPullRequestSearchPullRequest.Id = dataStore.Connection.Insert(newPullRequestSearchPullRequest);
        return newPullRequestSearchPullRequest;
    }

    public static void DeleteUnreferenced(DataStore dataStore)
    {
        var sql = "DELETE FROM PullRequestSearchPullRequest WHERE (PullRequestSearch NOT IN (SELECT Id FROM PullRequestSearch)) OR (PullRequest NOT IN (SELECT Id FROM PullRequest))";
        var command = dataStore.Connection!.CreateCommand();
        command.CommandText = sql;
        var rowsDeleted = command.ExecuteNonQuery();
    }
}
