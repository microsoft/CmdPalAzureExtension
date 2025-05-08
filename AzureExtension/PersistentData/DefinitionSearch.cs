// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Client;
using AzureExtension.Controls;
using AzureExtension.Data;
using Dapper;
using Dapper.Contrib.Extensions;

namespace AzureExtension.PersistentData;

[Table("DefinitionSearch")]
public class DefinitionSearch : IDefinitionSearch
{
    [Key]
    public long Id { get; set; } = DataStore.NoForeignKey;

    public long InternalId { get; set; } = DataStore.NoForeignKey;

    public string ProjectUrl { get; set; } = string.Empty;

    public bool IsTopLevel { get; set; }

    public string Name => throw new NotImplementedException();

    public string Url => ProjectUrl;

    [Computed]
    [Write(false)]
    public AzureUri AzureUri
    {
        get
        {
            return new AzureUri(ProjectUrl);
        }
    }

    public static DefinitionSearch? Get(DataStore dataStore, long internalId, string projectUrl)
    {
        var sql = "SELECT * FROM DefinitionSearch WHERE InternalId = @InternalId AND ProjectUrl = @ProjectUrl";
        var definitionSearch = dataStore.Connection.QueryFirstOrDefault<DefinitionSearch>(sql, new { InternalId = internalId, ProjectUrl = projectUrl });
        return definitionSearch;
    }

    public static DefinitionSearch Add(DataStore dataStore, long internalId, string projectUrl)
    {
        var definitionSearch = new DefinitionSearch
        {
            InternalId = internalId,
            ProjectUrl = projectUrl,
        };
        dataStore.Connection.Insert(definitionSearch);
        return definitionSearch;
    }

    public static void Remove(DataStore dataStore, long internalId, string projectUrl)
    {
        var sql = "DELETE FROM DefinitionSearch WHERE InternalId = @InternalId AND ProjectUrl = @ProjectUrl";
        var command = dataStore.Connection!.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@InternalId", internalId);
        command.Parameters.AddWithValue("@ProjectUrl", projectUrl);
        var deleted = command.ExecuteNonQuery();
    }

    public static IEnumerable<IDefinitionSearch> GetAll(DataStore dataStore)
    {
        var sql = "SELECT * FROM DefinitionSearch";
        var definitionSearches = dataStore.Connection.Query<DefinitionSearch>(sql);
        return definitionSearches;
    }

    public static IEnumerable<IDefinitionSearch> GetTopLevel(DataStore dataStore)
    {
        var sql = "SELECT * FROM DefinitionSearch WHERE IsTopLevel = 1";
        var definitionSearches = dataStore.Connection.Query<DefinitionSearch>(sql);
        return definitionSearches;
    }

    public static void AddOrUpdate(DataStore dataStore, long internalId, string projectUrl, bool isTopLevel)
    {
        var definitionSearch = Get(dataStore, internalId, projectUrl);
        definitionSearch ??= Add(dataStore, internalId, projectUrl);
        definitionSearch.IsTopLevel = isTopLevel;
        dataStore.Connection.Update(definitionSearch);
    }
}
