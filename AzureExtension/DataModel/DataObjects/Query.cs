// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls;
using AzureExtension.Data;
using AzureExtension.Helpers;
using Dapper;
using Dapper.Contrib.Extensions;
using Serilog;

namespace AzureExtension.DataModel;

[Table("Query")]
public class Query : IQuery
{
    private static readonly Lazy<ILogger> _logger = new(() => Serilog.Log.ForContext("SourceContext", $"DataModel/{nameof(Query)}"));

    private static readonly ILogger _log = _logger.Value;

    // This is the time between seeing a search and updating it's TimeUpdated.
    private static readonly long _updateThreshold = TimeSpan.FromMinutes(2).Ticks;

    [Key]
    public long Id { get; set; } = DataStore.NoForeignKey;

    // Guid
    public string QueryId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    // Key in Project table
    public long ProjectId { get; set; } = DataStore.NoForeignKey;

    // We need developer id because query results may be different depending on the user.
    public string Username { get; set; } = string.Empty;

    public long TimeUpdated { get; set; } = DataStore.NoForeignKey;

    public override string ToString() => QueryId;

    [Write(false)]
    private DataStore? DataStore { get; set; }

    [Write(false)]
    [Computed]
    public Project Project => Project.Get(DataStore, ProjectId);

    [Write(false)]
    [Computed]
    public DateTime UpdatedAt => TimeUpdated.ToDateTime();

    [Write(false)]
    [Computed]
    public string Name => DisplayName;

    [Write(false)]
    [Computed]
    public string Url => QueryId;

    private static Query Create(string queryId, long projectId, string username, string displayName)
    {
        return new Query
        {
            QueryId = queryId,
            ProjectId = projectId,
            Username = username,
            DisplayName = displayName,
            TimeUpdated = DateTime.UtcNow.ToDataStoreInteger(),
        };
    }

    private static Query AddOrUpdate(DataStore dataStore, Query query)
    {
        var existing = Get(dataStore, query.QueryId, query.Username);
        if (existing is not null)
        {
            if ((query.TimeUpdated - existing.TimeUpdated) > _updateThreshold)
            {
                query.Id = existing.Id;
                dataStore.Connection!.Update(query);
                return query;
            }
            else
            {
                return existing;
            }
        }

        // No existing search, add it.
        query.Id = dataStore.Connection!.Insert(query);
        return query;
    }

    // Direct Get always returns a non-null object for reference in other objects.
    public static Query Get(DataStore dataStore, long id)
    {
        if (dataStore == null)
        {
            return new Query();
        }

        var query = dataStore.Connection!.Get<Query>(id);
        if (query != null)
        {
            query.DataStore = dataStore;
        }

        return query ?? new Query();
    }

    // Query is unique on queryId and developerId
    public static Query? Get(DataStore dataStore, string queryId, string username)
    {
        var sql = @"SELECT * FROM Query WHERE QueryId = @QueryId AND Username = @Username;";
        var param = new
        {
            QueryId = queryId,
            Username = username,
        };

        var query = dataStore.Connection!.QueryFirstOrDefault<Query>(sql, param, null);
        if (query is not null)
        {
            query.DataStore = dataStore;
        }

        return query;
    }

    public static Query GetOrCreate(DataStore dataStore, string queryId, long projectId, string username, string displayName)
    {
        var newQuery = Create(queryId, projectId, username, displayName);
        return AddOrUpdate(dataStore, newQuery);
    }

    public static void DeleteBefore(DataStore dataStore, DateTime date)
    {
        // Delete queries older than the date listed.
        var sql = @"DELETE FROM Query WHERE TimeUpdated < $Time;";
        var command = dataStore.Connection!.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$Time", date.ToDataStoreInteger());
        _log.Debug(DataStore.GetCommandLogMessage(sql, command));
        var rowsDeleted = command.ExecuteNonQuery();
        _log.Debug(DataStore.GetDeletedLogMessage(rowsDeleted));
    }
}
