// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls;
using AzureExtension.Data;
using AzureExtension.Helpers;
using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace AzureExtension.DataModel;

[Table("Definition")]
public class Definition
{
    private static readonly long _updateThreshold = TimeSpan.FromHours(4).Ticks;

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
            HtmlUrl = definitionReference.Url,
            TimeUpdated = DateTime.UtcNow.ToDataStoreInteger(),
        };
        definition.DataStore = dataStore;
        return definition;
    }

    private static Definition? GetByInternalId(DataStore dataStore, long internalId)
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
            if (definition.CreationDate - existingDefinition.CreationDate < _updateThreshold)
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

    public static void DeleteUnreferenced(DataStore dataStore)
    {
        var sql = "DELETE FROM Definition WHERE ProjectId NOT IN (SELECT Id FROM Project)";
        var command = dataStore.Connection!.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}
