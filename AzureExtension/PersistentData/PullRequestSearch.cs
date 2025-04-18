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

[Table("PullRequestSearch")]
public class PullRequestSearch : IPullRequestSearch
{
    private static readonly Lazy<ILogger> _logger = new(() => Log.ForContext("SourceContext", $"PersistentData/{nameof(PullRequestSearch)}"));

    private static readonly ILogger _log = _logger.Value;

    [Key]
    public long Id { get; set; } = DataStore.NoForeignKey;

    public string Url { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string View { get; set; } = string.Empty;

    public bool IsTopLevel { get; set; }

    [Write(false)]
    public AzureSearchType Type { get; set; } = AzureSearchType.PullRequestSearch;

    [Computed]
    [Write(false)]
    public AzureUri AzureUri
    {
        get
        {
            return new AzureUri(Url);
        }
    }

    public static PullRequestSearch? Get(DataStore dataStore, string url, string name, string view)
    {
        var sql = "SELECT * FROM PullRequestSearch WHERE Url = @Url AND Name = @Name AND View = @View";
        var pullRequestSearch = dataStore.Connection.QueryFirstOrDefault<PullRequestSearch>(sql, new { Url = url, Name = name, View = view });
        return pullRequestSearch;
    }

    public static PullRequestSearch Add(DataStore dataStore, string url, string name, string view)
    {
        var pullRequestSearch = new PullRequestSearch
        {
            Url = url,
            Name = name,
            View = view,
        };

        dataStore.Connection.Insert(pullRequestSearch);
        return pullRequestSearch;
    }

    public static void Remove(DataStore dataStore, string url, string name, string view)
    {
        var sql = "DELETE FROM PullRequestSearch WHERE Url = @Url AND Name = @Name AND View = @View";
        var command = dataStore.Connection!.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@Url", url);
        command.Parameters.AddWithValue("@Name", name);
        command.Parameters.AddWithValue("@View", view);
        _log.Verbose(DataStore.GetCommandLogMessage(sql, command));
        var deleted = command.ExecuteNonQuery();
        _log.Verbose($"Deleted {deleted} rows from PullRequestSearch table.");
    }

    public static IEnumerable<IPullRequestSearch> GetAll(DataStore dataStore)
    {
        var sql = "SELECT * FROM PullRequestSearch";
        var pullRequestSearches = dataStore.Connection.Query<PullRequestSearch>(sql);
        return pullRequestSearches;
    }

    public static IEnumerable<IPullRequestSearch> GetTopLevel(DataStore dataStore)
    {
        var sql = "SELECT * FROM PullRequestSearch WHERE IsTopLevel = 1";
        var pullRequestSearches = dataStore.Connection.Query<PullRequestSearch>(sql);
        return pullRequestSearches;
    }

    public static void AddOrUpdate(DataStore dataStore, string url, string title, string view, bool isTopLevel)
    {
        var pullRequestSearch = Get(dataStore, url, title, view);

        pullRequestSearch ??= Add(dataStore, url, title, view);

        pullRequestSearch.IsTopLevel = isTopLevel;

        dataStore.Connection.Update(pullRequestSearch);
    }
}
