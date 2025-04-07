// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Dynamic;
using System.Text.Json;
using AzureExtension.Client;
using AzureExtension.DataModel;
using AzureExtension.DeveloperId;
using AzureExtension.Helpers;
using Microsoft.Azure.Pipelines.WebApi;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using Serilog;
using TFModels = Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace AzureExtension.Controls.Pages;

public class SearchPage<T> : ListPage
{
    private readonly ILogger _log;

    public ISearch CurrentSearch { get; private set; }

    protected IResources Resources { get; private set; }

    private IDeveloperIdProvider DeveloperIdProvider { get; set; }

    private IDeveloperId _developerId;

    // Max number of query results to fetch for a given query.
    public static readonly int QueryResultLimit = 25;

    // Connections are a pairing of DeveloperId and a Uri.
    private static readonly ConcurrentDictionary<Tuple<Uri, IDeveloperId>, VssConnection> _connections = new();

    // Search is mandatory for this page to exist
    public SearchPage(ISearch search, IResources resources, IDeveloperIdProvider developerIdProvider)
    {
        Icon = new IconInfo(AzureIcon.IconDictionary["logo"]);
        Name = search.Name;
        CurrentSearch = search;
        Resources = resources;
        _log = Serilog.Log.ForContext("SourceContext", $"AzureExtension/Controls/Pages/{nameof(SearchPage<T>)}");
        DeveloperIdProvider = developerIdProvider;
        _developerId = developerIdProvider.GetLoggedInDeveloperIdsInternal().FirstOrDefault() ?? throw new ArgumentNullException(nameof(developerIdProvider));
    }

    public override IListItem[] GetItems() => DoGetItems(SearchText).GetAwaiter().GetResult();

    private async Task<IListItem[]> DoGetItems(string query)
    {
        try
        {
            _log.Information($"Getting items for search query \"{CurrentSearch.Name}\"");
            var items = await GetSearchItems();

            var iconString = "logo";

            if (items.Any())
            {
                return items.Select(item => GetListItem(item!)).ToArray();
            }
            else
            {
                return !items.Any()
                    ? new ListItem[]
                    {
                            new(new NoOpCommand())
                            {
                                Title = Resources.GetResource("Pages_No_Items_Found"),
                                Icon = new IconInfo(AzureIcon.IconDictionary[iconString]),
                            },
                    }
                    :
                    [
                            new ListItem(new NoOpCommand())
                            {
                                Title = Resources.GetResource("Pages_Error_Title"),
                                Details = new Details()
                                {
                                    Body = Resources.GetResource("Pages_Error_Body"),
                                },
                                Icon = new IconInfo(AzureIcon.IconDictionary[iconString]),
                            },
                    ];
            }
        }
        catch (Exception ex)
        {
            return
            [
                    new ListItem(new NoOpCommand())
                    {
                        Title = Resources.GetResource("Pages_Error_Title"),
                        Details = new Details()
                        {
                            Title = ex.Message,
                            Body = string.IsNullOrEmpty(ex.StackTrace) ? "There is no stack trace for the error." : ex.StackTrace,
                        },
                    },
            ];
        }
    }

    public ListItem GetListItem(TFModels.WorkItem item) => new ListItem(new NoOpCommand())
    {
        Title = item.Fields["System.Title"].ToString() ?? item.Fields["System.Id"].ToString() ?? string.Empty,
        Icon = new IconInfo(AzureIcon.IconDictionary["logo"]),
    };

    private async Task<IEnumerable<TFModels.WorkItem>> GetSearchItems()
    {
        if (!CurrentSearch.Uri!.IsQuery)
        {
            throw new ArgumentException($"Query is not a valid Uri Query: {CurrentSearch.Uri}");
        }

        var result = GetConnection(CurrentSearch.Uri.Connection, _developerId);
        if (result.Result != ResultType.Success)
        {
            if (result.Exception != null)
            {
                throw result.Exception;
            }
            else
            {
                throw new AzureAuthorizationException($"Failed getting connection: {CurrentSearch.Uri.Connection} for {_developerId.LoginId} with {result.Error}");
            }
        }

        var witClient = result.Connection!.GetClient<WorkItemTrackingHttpClient>();
        if (witClient == null)
        {
            throw new AzureClientException($"Failed getting WorkItemTrackingHttpClient for {_developerId.LoginId} and {CurrentSearch.Uri.Connection}");
        }

        // Good practice to only create data after we know the client is valid, but any exceptions
        // will roll back the transaction.
        var org = Organization.Create(result.Connection.Uri);
        if (org == null)
        {
            throw new DataStoreException($"Organization.GetOrCreate failed for: {CurrentSearch.Uri.Connection}");
        }

        var teamProject = GetTeamProject(CurrentSearch.Uri.Project, _developerId, CurrentSearch.Uri.Connection);
        var project = CreateFromTeamProject(teamProject, org.Id);

        var getQueryResult = await witClient.GetQueryAsync(project.InternalId, CurrentSearch.Uri.Query);
        if (getQueryResult == null)
        {
            throw new AzureClientException($"GetQueryAsync failed for {CurrentSearch.Uri.Connection}, {project.InternalId}, {CurrentSearch.Uri.Query}");
        }

        var queryId = new Guid(CurrentSearch.Uri.Query);
        var count = await witClient.GetQueryResultCountAsync(project.Name, queryId);
        var queryResult = await witClient.QueryByIdAsync(project.InternalId, queryId);
        if (queryResult == null)
        {
            throw new AzureClientException($"QueryByIdAsync failed for {CurrentSearch.Uri.Connection}, {project.InternalId}, {queryId}");
        }

        var workItemIds = new List<int>();

        // The WorkItems collection and individual reference objects may be null.
        switch (queryResult.QueryType)
        {
            // Tree types are treated as flat, but the data structure is different.
            case TFModels.QueryType.Tree:
                if (queryResult.WorkItemRelations is not null)
                {
                    foreach (var workItemRelation in queryResult.WorkItemRelations)
                    {
                        if (workItemRelation is null || workItemRelation.Target is null)
                        {
                            continue;
                        }

                        workItemIds.Add(workItemRelation.Target.Id);
                        if (workItemIds.Count >= QueryResultLimit)
                        {
                            break;
                        }
                    }
                }

                break;

            case TFModels.QueryType.Flat:
                if (queryResult.WorkItems is not null)
                {
                    foreach (var item in queryResult.WorkItems)
                    {
                        if (item is null)
                        {
                            continue;
                        }

                        workItemIds.Add(item.Id);
                        if (workItemIds.Count >= QueryResultLimit)
                        {
                            break;
                        }
                    }
                }

                break;

            case TFModels.QueryType.OneHop:

                // OneHop work item structure is the same as the tree type.
                goto case TFModels.QueryType.Tree;

            default:
                _log.Warning($"Found unhandled QueryType: {queryResult.QueryType} for query: {queryId}");
                break;
        }

        var workItems = new List<TFModels.WorkItem>();
        if (workItemIds.Count > 0)
        {
            workItems = await witClient.GetWorkItemsAsync(project.InternalId, workItemIds, null, null, TFModels.WorkItemExpand.Links, TFModels.WorkItemErrorPolicy.Omit);
            if (workItems == null)
            {
                throw new AzureClientException($"GetWorkItemsAsync failed for {CurrentSearch.Uri.Connection}, {project.InternalId}, Ids: {string.Join(",", workItemIds.ToArray())}");
            }
        }

        return workItems;
    }

    // Connections can go bad and throw VssUnauthorizedException after some time, even if
    // if HasAuthenticated is true. The forceNewConnection default parameter is set to true until
    // the bad connection issue can be reliably detected and solved.
    public ConnectionResult GetConnection(Uri connectionUri, IDeveloperId developerId, bool forceNewConnection = true)
    {
        VssConnection? connection;
        var connectionKey = Tuple.Create(connectionUri, developerId);
        if (_connections.ContainsKey(connectionKey))
        {
            if (_connections.TryGetValue(connectionKey, out connection))
            {
                // If not forcing a new connection and it has authenticated, reuse it.
                if (!forceNewConnection && connection.HasAuthenticated)
                {
                    _log.Debug($"Retrieving valid connection to {connectionUri} with {developerId.LoginId}");
                    return new ConnectionResult(connectionUri, null, connection);
                }
                else
                {
                    // Remove the bad connection.
                    if (_connections.TryRemove(new KeyValuePair<Tuple<Uri, IDeveloperId>, VssConnection>(connectionKey, connection)))
                    {
                        _log.Information($"Removed bad connection to {connectionUri} with {developerId.LoginId}");
                    }
                    else
                    {
                        // Something else may have removed it first, that's probably OK, but _log it
                        // in case it isn't as it may be a symptom of a larger problem.
                        _log.Warning($"Failed to remove bad connection to {connectionUri} with {developerId.LoginId}");
                    }
                }
            }
        }

        // Either connection was bad or it doesn't exist.
        var result = AzureClientProvider.CreateVssConnection(connectionUri, developerId);
        if (result.Result == ResultType.Success)
        {
            if (_connections.TryAdd(connectionKey, result.Connection!))
            {
                _log.Debug($"Added connection for {connectionUri} with {developerId.LoginId}");
            }
            else
            {
                _log.Warning($"Failed to add connection for {connectionUri} with {developerId.LoginId}");
            }
        }

        return result;
    }

    private TeamProject GetTeamProject(string projectName, IDeveloperId developerId, Uri connection)
    {
        var result = GetConnection(connection, developerId);
        if (result.Result != ResultType.Success)
        {
            if (result.Exception != null)
            {
                throw result.Exception;
            }
            else
            {
                throw new AzureAuthorizationException($"Failed getting connection: {connection} for {developerId.LoginId} with {result.Error}");
            }
        }

        var projectClient = new ProjectHttpClient(result.Connection!.Uri, result.Connection!.Credentials);
        if (projectClient == null)
        {
            throw new AzureClientException($"Failed getting ProjectHttpClient for {connection}");
        }

        var project = projectClient.GetProject(projectName).Result;
        if (project == null)
        {
            throw new AzureClientException($"Project reference was null for {connection} and Project: {projectName}");
        }

        return project;
    }

    public Project CreateFromTeamProject(TeamProject project, long organizationId)
    {
        return new Project
        {
            InternalId = project.Id.ToString(),
            Name = project.Name ?? string.Empty,
            Description = project.Description ?? string.Empty,
            OrganizationId = organizationId,
            TimeUpdated = DateTime.UtcNow.ToDataStoreInteger(),
        };
    }
}
