// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls;
using AzureExtension.Data;
using AzureExtension.Helpers;
using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.TeamFoundation.Build.WebApi;

namespace AzureExtension.DataModel;

[Table("Definition")]
public class Definition : IDefinition
{
    private static readonly long _updateThreshold = TimeSpan.FromMinutes(1).Ticks;

    [Key]
    public long Id { get; set; } = DataStore.NoForeignKey;

    public long InternalId { get; set; } = DataStore.NoForeignKey;

    public string Name { get; set; } = string.Empty;

    public long ProjectId { get; set; } = DataStore.NoForeignKey;

    public long CreationDate { get; set; } = DataStore.NoForeignKey;

    public string HtmlUrl { get; set; } = string.Empty;

    public long TimeUpdated { get; set; } = DataStore.NoForeignKey;

    [Write(false)]
    private DataStore? DataStore { get; set; }

    [Write(false)]
    [Computed]
    public Project Project => Project.Get(DataStore, ProjectId);

    [Write(false)]
    [Computed]
    public IBuild? MostRecentBuild => Build.GetForDefinition(DataStore, Id).FirstOrDefault();

    [Write(false)]
    [Computed]
    public DateTime UpdatedAt => TimeUpdated.ToDateTime();

    private static Definition Create(
        DataStore dataStore,
        DefinitionReference definitionReference,
        long projectId)
    {
        var definition = new Definition
        {
            InternalId = definitionReference.Id,
            Name = definitionReference.Name,
            ProjectId = projectId,
            CreationDate = definitionReference.CreatedDate.ToDataStoreInteger(),
            HtmlUrl = CreateDefinitionHtmlUrl(definitionReference.Url, definitionReference.Project.Name, definitionReference.Id),
            TimeUpdated = DateTime.UtcNow.ToDataStoreInteger(),
        };
        definition.DataStore = dataStore;
        return definition;
    }

    public static Definition? GetByInternalId(DataStore dataStore, long internalId)
    {
        var sql = "SELECT * FROM Definition WHERE InternalId = @InternalId";
        var definition = dataStore.Connection.QuerySingleOrDefault<Definition>(sql, new { InternalId = internalId });

        if (definition != null)
        {
            definition.DataStore = dataStore;
        }

        return definition;
    }

    public static Definition AddOrUpdate(DataStore dataStore, Definition definition)
    {
        var existingDefinition = GetByInternalId(dataStore, definition.InternalId);
        if (existingDefinition != null)
        {
            if (definition.TimeUpdated - existingDefinition.TimeUpdated < _updateThreshold)
            {
                return existingDefinition;
            }

            definition.Id = existingDefinition.Id;
            dataStore.Connection.Update(definition);
            return existingDefinition;
        }

        definition.DataStore = dataStore;
        definition.Id = dataStore.Connection.Insert(definition);
        return definition;
    }

    public static Definition GetOrCreate(
        DataStore dataStore,
        DefinitionReference definitionReference,
        long projectId)
    {
        var definition = Create(dataStore, definitionReference, projectId);
        return AddOrUpdate(dataStore, definition);
    }

    public static IEnumerable<Definition> GetAll(DataStore dataStore, long projectId)
    {
        var sql = "SELECT * FROM Definition WHERE ProjectId = @ProjectId";
        var param = new
        {
            ProjectId = projectId,
        };

        var definitions = dataStore.Connection.Query<Definition>(sql, param);
        foreach (var definition in definitions)
        {
            definition.DataStore = dataStore;
        }

        return definitions;
    }

    public static void DeleteUnreferenced(DataStore dataStore)
    {
        var sql = "DELETE FROM Definition WHERE (Id NOT IN (SELECT DISTINCT DefinitionId FROM Build))";
        var command = dataStore.Connection!.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private static string CreateDefinitionHtmlUrl(string url, string projectName, long definitionId)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("URL cannot be null or empty.", nameof(url));
        }

        try
        {
            var uri = new Uri(url);

            var segments = uri.Segments;
            if (segments.Length < 4)
            {
                throw new InvalidOperationException("The URL does not have the expected structure.");
            }

            var organization = segments[1].TrimEnd('/');

            return $"https://dev.azure.com/{organization}/{projectName}/_build?definitionId={definitionId}";
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to convert the URL to the desired format.", ex);
        }
    }
}
