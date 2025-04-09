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

    public ListItem GetListItem(JsonObject item)
    {
        var title = item["title"]?.GetValue<string>() ?? string.Empty;
        var iconBase64 = item["icon"]?.GetValue<string>() ?? string.Empty;
        var url = item["url"]?.GetValue<string>() ?? string.Empty;

        return new ListItem(new LinkCommand(url, _resources))
        {
            Title = title,
            Icon = new IconInfo(iconBase64),
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

    public Task<IEnumerable<JsonObject>> LoadContentData()
    {
        return GetQueryResult();
    }

    private async Task<IEnumerable<JsonObject>> GetQueryResult()
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

        // Convert all work items to Json based on fields provided in RequestOptions.
        dynamic workItemsObj = new ExpandoObject();
        var workItemsObjDict = (IDictionary<string, object>)workItemsObj;
        foreach (var workItem in workItems)
        {
            dynamic workItemObj = new ExpandoObject();
            var workItemObjFields = (IDictionary<string, object>)workItemObj;

            // System.Id is excluded from the query result.
            workItemObjFields.Add(AzureDataManager.SystemIdFieldName, workItem.Id!);

            var htmlUrl = Links.GetLinkHDref(workItem.Links, "html");
            workItemObjFields.Add(AzureDataManager.WorkItemHtmlUrlFieldName, htmlUrl);

            var requestOptions = RequestOptions.RequestOptionsDefault();

            foreach (var field in requestOptions.Fields)
            {
                // Ensure we do not try to add duplicate fields.
                if (workItemObjFields.ContainsKey(field))
                {
                    ExtensionHost.LogMessage($"Duplicate field '{field} in RequestOptions.Fields: {string.Join(", ", requestOptions.Fields.ToArray())}");
                    continue;
                }

                if (!workItem.Fields.ContainsKey(field))
                {
                    workItemObjFields.Add(field, string.Empty);
                    continue;
                }

                var fieldValue = workItem.Fields[field].ToString();
                if (fieldValue is null)
                {
                    workItemObjFields.Add(field, string.Empty);
                    continue;
                }

                if (workItem.Fields[field] is DateTime dateTime)
                {
                    // If we have a datetime object, convert it to ticks for easy conversion
                    // to a DateTime object in whatever local format the user is in.
                    workItemObjFields.Add(field, dateTime.Ticks);
                    continue;
                }

                var fieldIdentityRef = workItem.Fields[field] as IdentityRef;
                if (fieldValue == AzureDataManager.IdentityRefFieldValueName && fieldIdentityRef != null)
                {
                    var identity = Identity.CreateFromIdentityRef(fieldIdentityRef, result.Connection);
                    workItemObjFields.Add(field, identity.Id);
                    continue;
                }

                if (field == AzureDataManager.WorkItemTypeFieldName)
                {
                    // Need a separate query to create WorkItemType object.
                    var workItemTypeInfo = await witClient!.GetWorkItemTypeAsync(project.InternalId, fieldValue);
                    var workItemType = WorkItemType.CreateFromTeamWorkItemType(workItemTypeInfo, project.Id);

                    workItemObjFields.Add(field, workItemType.Id);
                    continue;
                }

                workItemObjFields.Add(field, workItem.Fields[field].ToString()!);
            }

            workItemsObjDict.Add(workItem.Id.ToStringInvariant(), workItemObj);
        }

        JsonSerializerOptions serializerOptions = new()
        {
#if DEBUG
            WriteIndented = true,
#else
            WriteIndented = false,
#endif
        };

        var serializedJson = System.Text.Json.JsonSerializer.Serialize(workItemsObj, serializerOptions);

        var itemsArray = new JsonArray();

        foreach (var element in serializedJson)
        {
            var workItem = JsonNode.Parse(element.Value.ToStringInvariant());

            if (workItem != null)
            {
                var dateTicks = workItem["System.ChangedDate"]?.GetValue<long>() ?? DateTime.UtcNow.Ticks;
                var dateTime = dateTicks.ToDateTime();
                var creator = _azureDataManager.GetIdentity(workItem["System.CreatedBy"]?.GetValue<long>() ?? 0L);
                var workItemType = _azureDataManager.GetWorkItemType(workItem["System.WorkItemType"]?.GetValue<long>() ?? 0L);

                var item = new JsonObject
            {
                { "title", workItem["System.Title"]?.GetValue<string>() ?? string.Empty },
                { "url", workItem[AzureDataManager.WorkItemHtmlUrlFieldName]?.GetValue<string>() ?? string.Empty },
                { "icon", GetIconForType(workItemType.Name) },
                { "status_icon", GetIconForStatusState(workItem["System.State"]?.GetValue<string>()) },
                { "number", element.Key },
                { "date", _timeSpanHelper.DateTimeOffsetToDisplayString(dateTime, Log) },
                { "user", creator.Name },
                { "status", workItem["System.State"]?.GetValue<string>() ?? string.Empty },
                { "avatar", creator.Avatar },
            };

                itemsArray.Add(item);
            }
        }

        // convert JsonArray into a List of JsonObject
        var itemsList = new List<JsonObject>();
        foreach (var item in itemsArray)
        {
            if (item is JsonObject jsonObject)
            {
                itemsList.Add(jsonObject);
            }
        }

        return itemsList;
    }
}
