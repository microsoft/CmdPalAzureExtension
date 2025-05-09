// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls;
using AzureExtension.Data;
using AzureExtension.Helpers;
using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.VisualStudio.Services.Common;
using TFModels = Microsoft.TeamFoundation.Build.WebApi;

namespace AzureExtension.DataModel;

[Table("Build")]
public class Build : IBuild
{
    [Key]
    public long Id { get; set; } = DataStore.NoForeignKey;

    public long InternalId { get; set; } = DataStore.NoForeignKey;

    public string BuildNumber { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Result { get; set; } = string.Empty;

    public long QueueTime { get; set; } = DataStore.NoForeignKey;

    public long StartTime { get; set; } = DataStore.NoForeignKey;

    public long FinishTime { get; set; } = DataStore.NoForeignKey;

    public string Url { get; set; } = string.Empty;

    public long DefinitionId { get; set; } = DataStore.NoForeignKey;

    public string SourceBranch { get; set; } = string.Empty;

    public string TriggerMessage { get; set; } = string.Empty;

    public long RequesterId { get; set; } = DataStore.NoForeignKey;

    public long TimeUpdated { get; set; } = DataStore.NoForeignKey;

    [Write(false)]
    private DataStore? DataStore { get; set; }

    [Write(false)]
    [Computed]
    public Identity? Requester => Identity.Get(DataStore, RequesterId);

    private static Build Create(DataStore dataStore, TFModels.Build tfBuild, long definitionId, long requesterId)
    {
        var build = new Build
        {
            InternalId = tfBuild.Id,
            BuildNumber = tfBuild.BuildNumber,
            Status = tfBuild.Status.ToString() ?? string.Empty,
            Result = tfBuild.Result.ToString() ?? string.Empty,
            QueueTime = tfBuild.QueueTime?.ToDataStoreInteger() ?? DataStore.NoForeignKey,
            StartTime = tfBuild.StartTime?.ToDataStoreInteger() ?? DataStore.NoForeignKey,
            FinishTime = tfBuild.FinishTime?.ToDataStoreInteger() ?? DataStore.NoForeignKey,
            Url = ConvertBuildUrlToHtmlUrl(tfBuild.Url, tfBuild.Project.Name, tfBuild.Id),
            DefinitionId = definitionId,
            SourceBranch = tfBuild.SourceBranch,
            TriggerMessage = tfBuild.TriggerInfo.GetValueOrDefault("ci.message", string.Empty),
            RequesterId = requesterId,
            TimeUpdated = DateTime.UtcNow.ToDataStoreInteger(),
        };

        return build;
    }

    private static Build? GetByInternalId(DataStore dataStore, long internalId)
    {
        var sql = @"SELECT * FROM Build WHERE InternalId = @InternalId";
        var param = new
        {
            InternalId = internalId,
        };

        var build = dataStore.Connection!.QueryFirstOrDefault<Build>(sql, param, null);

        if (build != null)
        {
            build.DataStore = dataStore;
        }

        return build;
    }

    private static Build AddOrUpdate(DataStore dataStore, Build build)
    {
        var existingBuild = GetByInternalId(dataStore, build.InternalId);
        if (existingBuild != null)
        {
            build.Id = existingBuild.Id;
            dataStore.Connection.Update(build);
            build.DataStore = dataStore;
            return build;
        }

        build.Id = dataStore.Connection.Insert(build);
        build.DataStore = dataStore;
        return build;
    }

    public static Build GetOrCreate(DataStore dataStore, TFModels.Build tfBuild, long definitionId, long requesterId)
    {
        var newBuild = Create(dataStore, tfBuild, definitionId, requesterId);
        return AddOrUpdate(dataStore, newBuild);
    }

    public static IEnumerable<Build> GetForDefinition(DataStore? dataStore, long definitionId)
    {
        if (dataStore == null)
        {
            return [];
        }

        var sql = @"SELECT * FROM Build WHERE DefinitionId = @DefinitionId ORDER BY TimeUpdated ASC";
        var param = new
        {
            DefinitionId = definitionId,
        };
        var builds = dataStore.Connection!.Query<Build>(sql, param, null);
        foreach (var build in builds)
        {
            build.DataStore = dataStore;
        }

        return builds;
    }

    public static void DeleteBefore(DataStore dataStore, DateTime date)
    {
        var sql = "DELETE FROM Build WHERE TimeUpdated < $Time";
        var command = dataStore.Connection!.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$Time", date.ToDataStoreInteger());
        var rowsDeleted = command.ExecuteNonQuery();
    }

    public static string ConvertBuildUrlToHtmlUrl(string url, string projectName, long buildId)
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

            return $"https://dev.azure.com/{organization}/{projectName}/_build/results?buildId={buildId}&view=results";
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to convert the URL to the desired format.", ex);
        }
    }
}
