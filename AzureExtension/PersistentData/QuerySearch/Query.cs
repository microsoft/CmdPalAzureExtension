// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Client;
using AzureExtension.Controls;
using AzureExtension.Data;
using Dapper;
using Dapper.Contrib.Extensions;
using Serilog;

namespace AzureExtension.PersistentData;

[Table("Query")]
public class Query : IQuery
{
    private static readonly Lazy<ILogger> _logger = new(() => Log.ForContext("SourceContext", $"PersistentData/{nameof(Query)}"));

    private static readonly ILogger _log = _logger.Value;

    [Key]
    public long Id { get; set; } = DataStore.NoForeignKey;

    // SearchString to satisfy ISearch interface
    public string Url { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool IsTopLevel { get; set; }

    [Computed]
    [Write(false)]
    public AzureUri AzureUri
    {
        get
        {
            return new AzureUri(Url);
        }
    }

    public static Query Add(string name, string url, bool isTopLevel)
    {
        return new Query
        {
            Name = name,
            Url = url,
            IsTopLevel = isTopLevel,
        };
    }

    public static Query? Get(DataStore datastore, string name, string url)
    {
        var sql = "SELECT * FROM Query WHERE Name = @Name AND Url = @Url";
        var query = datastore.Connection.QueryFirstOrDefault<Query>(sql, new { Name = name, Url = url });
        return query;
    }

    public static Query Add(DataStore datastore, string name, string url, bool isTopLevel)
    {
        var query = new Query
        {
            Name = name,
            Url = url,
            IsTopLevel = isTopLevel,
        };

        datastore.Connection.Insert(query);
        return query;
    }

    public static void Remove(DataStore datastore, string name, string url)
    {
        var sql = "DELETE FROM Query WHERE Name = @Name AND Url = @Url";
        var command = datastore.Connection!.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@Name", name);
        command.Parameters.AddWithValue("@Url", url);
        _log.Verbose(DataStore.GetCommandLogMessage(sql, command));
        var deleted = command.ExecuteNonQuery();
        _log.Verbose($"Deleted {deleted} rows from Query table.");
    }

    public static IEnumerable<IQuery> GetAll(DataStore datastore)
    {
        var sql = "SELECT * FROM Query";
        var query = datastore.Connection.Query<Query>(sql);
        return query;
    }

    public static IEnumerable<IQuery> GetTopLevel(DataStore datastore)
    {
        var sql = "SELECT * FROM Query WHERE IsTopLevel = 1";
        var query = datastore.Connection.Query<Query>(sql);
        return query;
    }

    public static void AddOrUpdate(DataStore datastore, string name, string url, bool isTopLevel)
    {
        var query = Get(datastore, name, url);

        query ??= Add(datastore, name, url, isTopLevel);

        query.IsTopLevel = isTopLevel;

        datastore.Connection.Update(query);
    }
}
