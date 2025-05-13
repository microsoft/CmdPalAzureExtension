// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Client;
using AzureExtension.Controls;
using AzureExtension.Data;
using AzureExtension.Helpers;
using Dapper;
using Dapper.Contrib.Extensions;

namespace AzureExtension.PersistentData;

[Table("DefinitionSearch")]
public class DefinitionSearch : IPipelineDefinitionSearch
{
    [Key]
    public long Id { get; set; } = DataStore.NoForeignKey;

    public long InternalId { get; set; } = DataStore.NoForeignKey;

    public string Url { get; set; } = string.Empty;

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

    [Write(false)]
    public string Name => InternalId.ToStringInvariant();

    public static DefinitionSearch? Get(DataStore dataStore, long internalId, string url)
    {
        var sql = "SELECT * FROM DefinitionSearch WHERE InternalId = @InternalId AND Url = @Url";
        var definitionSearch = dataStore.Connection.QueryFirstOrDefault<DefinitionSearch>(sql, new { InternalId = internalId, Url = url });
        return definitionSearch;
    }

    public static DefinitionSearch Add(DataStore dataStore, long internalId, string url)
    {
        var definitionSearch = new DefinitionSearch
        {
            InternalId = internalId,
            Url = url,
        };
        dataStore.Connection.Insert(definitionSearch);
        return definitionSearch;
    }

    public static void Remove(DataStore dataStore, long internalId, string url)
    {
        var sql = "DELETE FROM DefinitionSearch WHERE InternalId = @InternalId AND Url = @Url";
        var command = dataStore.Connection!.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@InternalId", internalId);
        command.Parameters.AddWithValue("@Url", url);
        var deleted = command.ExecuteNonQuery();
    }

    public static IEnumerable<IPipelineDefinitionSearch> GetAll(DataStore dataStore)
    {
        var sql = "SELECT * FROM DefinitionSearch";
        var definitionSearches = dataStore.Connection.Query<DefinitionSearch>(sql);
        return definitionSearches;
    }

    public static IEnumerable<IPipelineDefinitionSearch> GetTopLevel(DataStore dataStore)
    {
        var sql = "SELECT * FROM DefinitionSearch WHERE IsTopLevel = 1";
        var definitionSearches = dataStore.Connection.Query<DefinitionSearch>(sql);
        return definitionSearches;
    }

    public static void AddOrUpdate(DataStore dataStore, long internalId, string url, bool isTopLevel)
    {
        var definitionSearch = Get(dataStore, internalId, url);
        definitionSearch ??= Add(dataStore, internalId, url);
        definitionSearch.IsTopLevel = isTopLevel;
        dataStore.Connection.Update(definitionSearch);
    }
}
