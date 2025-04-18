// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Data;
using AzureExtension.Helpers;
using Dapper;
using Dapper.Contrib.Extensions;
using Serilog;

namespace AzureExtension.DataModel;

[Table("PullRequestSearch")]
public class PullRequestSearch
{
    private static readonly Lazy<ILogger> _logger = new(() => Serilog.Log.ForContext("SourceContext", $"DataModel/{nameof(PullRequestSearch)}"));

    private static readonly ILogger _log = _logger.Value;

    // This is the time between seeing a search and updating it's TimeUpdated.
    private static readonly long _updateThreshold = TimeSpan.FromMinutes(2).Ticks;

    [Key]
    public long Id { get; set; } = DataStore.NoForeignKey;

    // Key in Repository table
    public long RepositoryId { get; set; } = DataStore.NoForeignKey;

    // Key in Project table
    public long ProjectId { get; set; } = DataStore.NoForeignKey;

    // We need developer id because pullRequests results may be different depending on the user.
    public string Username { get; set; } = string.Empty;

    public long ViewId { get; set; } = DataStore.NoForeignKey;

    public long TimeUpdated { get; set; } = DataStore.NoForeignKey;

    public override string ToString() => $"{Username}/{Repository.Name}";

    [Write(false)]
    private DataStore? DataStore { get; set; }

    [Write(false)]
    [Computed]
    public Project Project => Project.Get(DataStore, ProjectId);

    [Write(false)]
    [Computed]
    public Repository Repository => Repository.Get(DataStore, RepositoryId);

    [Write(false)]
    [Computed]
    public PullRequestView View => (PullRequestView)ViewId;

    [Write(false)]
    [Computed]
    public DateTime UpdatedAt => TimeUpdated.ToDateTime();

    private static PullRequestSearch Create(long repositoryId, long projectId, string developerLogin, PullRequestView view)
    {
        return new PullRequestSearch
        {
            RepositoryId = repositoryId,
            ProjectId = projectId,
            Username = developerLogin,
            ViewId = (long)view,
            TimeUpdated = DateTime.UtcNow.ToDataStoreInteger(),
        };
    }

    private static PullRequestSearch AddOrUpdate(DataStore dataStore, PullRequestSearch pullRequests)
    {
        var existing = Get(dataStore, pullRequests.ProjectId, pullRequests.RepositoryId, pullRequests.Username, pullRequests.View);
        if (existing is not null)
        {
            // Update threshold is in case there are many requests in a short period of time.
            if ((pullRequests.TimeUpdated - existing.TimeUpdated) > _updateThreshold)
            {
                pullRequests.Id = existing.Id;
                dataStore.Connection!.Update(pullRequests);
                return pullRequests;
            }
            else
            {
                return existing;
            }
        }

        // No existing search, add it.
        pullRequests.Id = dataStore.Connection!.Insert(pullRequests);
        return pullRequests;
    }

    // Direct Get always returns a non-null object for reference in other objects.
    public static PullRequestSearch Get(DataStore dataStore, long id)
    {
        if (dataStore == null)
        {
            return new PullRequestSearch();
        }

        var pullRequests = dataStore.Connection!.Get<PullRequestSearch>(id);
        if (pullRequests != null)
        {
            pullRequests.DataStore = dataStore;
        }

        return pullRequests ?? new PullRequestSearch();
    }

    public static PullRequestSearch? Get(DataStore dataStore, long projectId, long repositoryId, string username, PullRequestView view)
    {
        var sql = @"SELECT * FROM PullRequestSearch WHERE ProjectId = @ProjectId AND RepositoryId = @RepositoryId AND Username = @Username AND ViewId = @ViewId;";
        var param = new
        {
            ProjectId = projectId,
            RepositoryId = repositoryId,
            Username = username,
            ViewId = (long)view,
        };

        var pullRequests = dataStore.Connection!.QueryFirstOrDefault<PullRequestSearch>(sql, param, null);
        if (pullRequests is not null)
        {
            pullRequests.DataStore = dataStore;
        }

        return pullRequests;
    }

    // DeveloperPullRequests is unique on developerId, repositoryName, project, and organization.
    public static PullRequestSearch? Get(DataStore dataStore, string organizationName, string projectName, string repositoryName, string username, PullRequestView view)
    {
        // Since this also requires organization information and project is referenced by Id, we must first look up the project.
        var project = Project.Get(dataStore, projectName, organizationName);
        if (project == null)
        {
            return null;
        }

        var repository = Repository.Get(dataStore, project.Id, repositoryName);
        if (repository == null)
        {
            return null;
        }

        return Get(dataStore, project.Id, repository.Id, username, view);
    }

    public static IEnumerable<PullRequestSearch> GetAllForDeveloper(DataStore dataStore)
    {
        var sql = @"SELECT * FROM PullRequestSearch WHERE ViewId = @ViewId;";
        var param = new
        {
            ViewId = (long)PullRequestView.Mine,
        };

        var pullRequestsSet = dataStore.Connection!.Query<PullRequestSearch>(sql, param, null) ?? [];
        foreach (var pullRequestsEntry in pullRequestsSet)
        {
            pullRequestsEntry.DataStore = dataStore;
        }

        return pullRequestsSet;
    }

    public static PullRequestSearch GetOrCreate(DataStore dataStore, long repositoryId, long projectId, string developerId, PullRequestView view)
    {
        var newDeveloperPullRequests = Create(repositoryId, projectId, developerId, view);
        return AddOrUpdate(dataStore, newDeveloperPullRequests);
    }

    public static void DeleteBefore(DataStore dataStore, DateTime date)
    {
        // Delete queries older than the date listed.
        var sql = @"DELETE FROM PullRequestSearch WHERE TimeUpdated < $Time;";
        var command = dataStore.Connection!.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$Time", date.ToDataStoreInteger());
        _log.Debug(DataStore.GetCommandLogMessage(sql, command));
        var rowsDeleted = command.ExecuteNonQuery();
        _log.Debug(DataStore.GetDeletedLogMessage(rowsDeleted));
    }
}
