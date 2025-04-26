// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Data;
using AzureExtension.Helpers;
using Dapper;
using Dapper.Contrib.Extensions;

namespace AzureExtension.DataModel;

[Table("QueryWorkItem")]
public class QueryWorkItem
{
    [Key]
    public long Id { get; set; } = DataStore.NoForeignKey;

    public long TimeUpdated { get; set; } = DataStore.NoForeignKey;

    public long Query { get; set; } = DataStore.NoForeignKey;

    public long WorkItem { get; set; } = DataStore.NoForeignKey;

    [Write(false)]
    [Computed]
    public DateTime UpdatedAt => TimeUpdated.ToDateTime();

    private static QueryWorkItem GetByQueryIdAndWorkItemId(DataStore dataStore, long queryId, long workItemId)
    {
        var sql = "SELECT * FROM QueryWorkItem WHERE Query = @QueryId AND WorkItem = @WorkItemId";
        var param = new
        {
            QueryId = queryId,
            WorkItemId = workItemId,
        };
        var queryWorkItem = dataStore.Connection.QueryFirstOrDefault<QueryWorkItem>(sql, param, null);
        return queryWorkItem;
    }

    public static QueryWorkItem AddWorkItemToQuery(DataStore dataStore, long queryId, long workItemId)
    {
        var existingQueryWorkItem = GetByQueryIdAndWorkItemId(dataStore, queryId, workItemId);

        if (existingQueryWorkItem != null)
        {
            return existingQueryWorkItem;
        }

        var newQueryWorkItem = new QueryWorkItem
        {
            Query = queryId,
            WorkItem = workItemId,
            TimeUpdated = DateTime.UtcNow.ToDataStoreInteger(),
        };
        newQueryWorkItem.Id = dataStore.Connection.Insert(newQueryWorkItem);
        return newQueryWorkItem;
    }

    public static void DeleteUnreferenced(DataStore dataStore)
    {
        var sql = "DELETE FROM QueryWorkItem WHERE (Query NOT IN (SELECT Id FROM Query)) OR (WorkItem NOT IN (SELECT Id FROM WorkItem))";
        var command = dataStore.Connection!.CreateCommand();
        command.CommandText = sql;
        var rowsDeleted = command.ExecuteNonQuery();
    }

    public static void DeleteBefore(DataStore dataStore, Query query, DateTime date)
    {
        var sql = "DELETE FROM QueryWorkItem WHERE Query = $QueryId AND TimeUpdated < $Time";
        var command = dataStore.Connection!.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$QueryId", query.Id);
        command.Parameters.AddWithValue("$Time", date.ToDataStoreInteger());
        var rowsDeleted = command.ExecuteNonQuery();
    }
}
