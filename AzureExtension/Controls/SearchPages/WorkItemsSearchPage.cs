// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Dynamic;
using System.Net;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json;
using System.Text.Json.Nodes;
using AzureExtension.Client;
using AzureExtension.Controls.Commands;
using AzureExtension.DataModel;
using AzureExtension.DeveloperId;
using AzureExtension.Helpers;
using AzureExtension.PersistentData;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using Newtonsoft.Json;
using Octokit;
using Serilog;
using TFModels = Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace AzureExtension.Controls.Pages;

public sealed partial class WorkItemsSearchPage : ListPage
{
    // Max number of query results to fetch for a given query.
    public static readonly int QueryResultLimit = 25;

    private readonly Lazy<ILogger> _log = new(() => Serilog.Log.ForContext("SourceContext", $"Pages/WorkItemsSearchPage"));

    private ILogger Log => _log.Value;

    // Connections are a pairing of DeveloperId and a Uri.
    private static readonly ConcurrentDictionary<Tuple<Uri, IDeveloperId>, VssConnection> _connections = new();

    private readonly IDeveloperId _developerId;

    private readonly QueryObject _queryObject;

    private readonly IResources _resources;

    private readonly AzureDataManager _azureDataManager;

    private readonly TimeSpanHelper _timeSpanHelper;

    public WorkItemsSearchPage(QueryObject queryObject, IDeveloperId developerId, IResources resources, AzureDataManager azureDataManager, TimeSpanHelper timeSpanHelper)
    {
        _developerId = developerId;
        _queryObject = queryObject;
        _resources = resources;
        Icon = new IconInfo(AzureIcon.IconDictionary["logo"]);
        Name = queryObject.Name;
        _azureDataManager = azureDataManager;
        _timeSpanHelper = timeSpanHelper;
    }

    public override IListItem[] GetItems() => DoGetItems(SearchText).GetAwaiter().GetResult();

    private async Task<IListItem[]> DoGetItems(string query)
    {
        var items = await LoadContentData();
        if (items != null && items.Any())
        {
            var listItems = new List<IListItem>();
            foreach (var item in items)
            {
                var listItem = GetListItem(item);
                listItems.Add(listItem);
            }

            return listItems.ToArray();
        }

        return Array.Empty<IListItem>();
    }

    public ListItem GetListItem(WorkItem item)
    {
        var title = item.SystemTitle;
        var url = item.HtmlUrl;

        return new ListItem(new LinkCommand(url, _resources))
        {
            Title = title,
            Icon = new IconInfo(AzureIcon.IconDictionary["logo"]),
        };
    }

    private string GetIconForType(string? workItemType)
    {
        return workItemType switch
        {
            "Bug" => IconLoader.GetIconAsBase64("Bug.png"),
            "Feature" => IconLoader.GetIconAsBase64("Feature.png"),
            "Issue" => IconLoader.GetIconAsBase64("Issue.png"),
            "Impediment" => IconLoader.GetIconAsBase64("Impediment.png"),
            "Pull Request" => IconLoader.GetIconAsBase64("PullRequest.png"),
            "Task" => IconLoader.GetIconAsBase64("Task.png"),
            _ => IconLoader.GetIconAsBase64("ADO.png"),
        };
    }

    private string GetIconForStatusState(string? statusState)
    {
        return statusState switch
        {
            "Closed" or "Completed" => IconLoader.GetIconAsBase64("StatusGreen.png"),
            "Committed" or "Resolved" or "Started" => IconLoader.GetIconAsBase64("StatusBlue.png"),
            _ => IconLoader.GetIconAsBase64("StatusGray.png"),
        };
    }

    public Task<IEnumerable<WorkItem>> LoadContentData()
    {
        return GetWorkItems();
    }

    private async Task<IEnumerable<WorkItem>> GetWorkItems()
    {
        var result = AzureDataManager.GetConnection(_queryObject.AzureUri.Connection, _developerId);

        if (result.Result != ResultType.Success)
        {
            if (result.Exception != null)
            {
                throw result.Exception;
            }
            else
            {
                throw new AzureAuthorizationException($"Failed getting connection: {_queryObject.AzureUri.Connection} with {result.Error}");
            }
        }

        var witClient = result.Connection!.GetClient<WorkItemTrackingHttpClient>();
        if (witClient == null)
        {
            throw new AzureClientException($"Failed getting WorkItemTrackingHttpClient");
        }

        // Good practice to only create data after we know the client is valid, but any exceptions
        // will roll back the transaction.
        var org = DataModel.Organization.Create(_queryObject.AzureUri.Connection);

        var teamProject = AzureDataManager.GetTeamProject(_queryObject.AzureUri.Project, _developerId, _queryObject.AzureUri.Connection);

        var project = DataModel.Project.CreateFromTeamProject(teamProject, org.Id);

        var getQueryResult = await witClient.GetQueryAsync(project.InternalId, _queryObject.AzureUri.Query);
        if (getQueryResult == null)
        {
            throw new AzureClientException($"GetQueryAsync failed for {_queryObject.AzureUri.Connection}, {project.InternalId}, {_queryObject.AzureUri.Query}");
        }

        var queryId = new Guid(_queryObject.AzureUri.Query);
        var count = await witClient.GetQueryResultCountAsync(project.Name, queryId);
        var queryResult = await witClient.QueryByIdAsync(project.InternalId, queryId);
        if (queryResult == null)
        {
            throw new AzureClientException($"QueryByIdAsync failed for {_queryObject.AzureUri.Connection}, {project.InternalId}, {queryId}");
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
                break;
        }

        var workItems = new List<TFModels.WorkItem>();
        if (workItemIds.Count > 0)
        {
            workItems = await witClient.GetWorkItemsAsync(project.InternalId, workItemIds, null, null, TFModels.WorkItemExpand.Links, TFModels.WorkItemErrorPolicy.Omit);
            if (workItems == null)
            {
                throw new AzureClientException($"GetWorkItemsAsync failed for {_queryObject.AzureUri.Connection}, {project.InternalId}, Ids: {string.Join(",", workItemIds.ToArray())}");
            }
        }

        var workItemsList = new List<WorkItem>();

        foreach (var workItem in workItems)
        {
            var cmdPalWorkItem = new WorkItem();

            cmdPalWorkItem.AddSystemId(workItem.Id);

            // cmdPalWorkItem.Icon = new IconInfo(GetIconForType(workItem.Fields[AzureDataManager.WorkItemTypeFieldName]?.ToString()));
            // cmdPalWorkItem.StatusIcon = new IconInfo(GetIconForStatusState(workItem.Fields["System.State"]?.ToString()));
            var htmlUrl = Links.GetLinkHref(workItem.Links, "html");
            cmdPalWorkItem.AddHtmlUrl(htmlUrl);

            var requestOptions = RequestOptions.RequestOptionsDefault();

            foreach (var field in requestOptions.Fields)
            {
                if (!workItem.Fields.ContainsKey(field))
                {
                    continue;
                }

                var fieldValue = workItem.Fields[field].ToString();
                if (fieldValue is null)
                {
                    continue;
                }

                if (workItem.Fields[field] is DateTime dateTime)
                {
                    if (field == "System.CreatedDate")
                    {
                        cmdPalWorkItem.SystemCreatedDate = dateTime.Ticks;
                    }
                    else if (field == "System.ChangedDate")
                    {
                        cmdPalWorkItem.SystemChangedDate = dateTime.Ticks;
                    }

                    continue;
                }

                var fieldIdentityRef = workItem.Fields[field] as IdentityRef;
                if (fieldValue == AzureDataManager.IdentityRefFieldValueName && fieldIdentityRef != null)
                {
                    var identity = Identity.CreateFromIdentityRef(fieldIdentityRef, result.Connection);

                    if (field == "System.CreatedBy")
                    {
                        cmdPalWorkItem.SystemCreatedBy = identity;
                    }
                    else if (field == "System.ChangedBy")
                    {
                        cmdPalWorkItem.SystemChangedBy = identity;
                    }

                    continue;
                }

                if (field == AzureDataManager.WorkItemTypeFieldName)
                {
                    // Need a separate query to create WorkItemType object.
                    var workItemTypeInfo = await witClient!.GetWorkItemTypeAsync(project.InternalId, fieldValue);
                    var workItemType = WorkItemType.CreateFromTeamWorkItemType(workItemTypeInfo, project.Id);

                    cmdPalWorkItem.SystemWorkItemType = workItemType;
                    continue;
                }

                if (field == "System.State")
                {
                    cmdPalWorkItem.SystemState = fieldValue;
                    continue;
                }

                if (field == "System.Reason")
                {
                    cmdPalWorkItem.SystemReason = fieldValue;
                    continue;
                }

                if (field == "System.Title")
                {
                    cmdPalWorkItem.SystemTitle = fieldValue;
                    continue;
                }
            }

            workItemsList.Add(cmdPalWorkItem);
        }

        return workItemsList;
    }
}
