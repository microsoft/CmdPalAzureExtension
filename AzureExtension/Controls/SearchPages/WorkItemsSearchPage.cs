// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json.Nodes;
using AzureExtension.Client;
using AzureExtension.Controls.Commands;
using AzureExtension.DataModel;
using AzureExtension.DeveloperId;
using AzureExtension.Helpers;
using AzureExtension.PersistentData;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.VisualStudio.Services.WebApi;
using Newtonsoft.Json;
using Serilog;
using TFModels = Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace AzureExtension.Controls.Pages;

public sealed partial class WorkItemsSearchPage(ISearch search, AzureDataManager azureDataManager, PersistentDataManager persistentDataManager, IResources resources, IDeveloperId devId, TimeSpanHelper timeSpanHelper)
    : SearchPage<JsonObject>(search, azureDataManager, persistentDataManager, resources, devId, timeSpanHelper)
{
    // Max number of query results to fetch for a given query.
    public static readonly int QueryResultLimit = 25;

    private readonly Lazy<ILogger> _log = new(() => Serilog.Log.ForContext("SourceContext", $"Pages/WorkItemsSearchPage"));

    private ILogger Log => _log.Value;

    // Connections are a pairing of DeveloperId and a Uri.
    private static readonly ConcurrentDictionary<Tuple<Uri, IDeveloperId>, VssConnection> _connections = new();

    public ListItem GetListItem(TFModels.WorkItem item) => new ListItem(new NoOpCommand())
    {
        Title = item.Fields["System.Title"].ToString() ?? item.Fields["System.Id"].ToString() ?? string.Empty,
        Icon = new IconInfo(AzureIcon.IconDictionary["logo"]),
    };

    private async Task<IEnumerable<TFModels.WorkItem>> GetSearchItems()
    {
        return await AzureDataManager.GetWorkItemsAsync(new AzureUri(CurrentSearch.SearchString), DeveloperId);
    }

    protected override ListItem GetListItem(JsonObject item)
    {
        var title = item["title"]?.GetValue<string>() ?? string.Empty;
        var iconBase64 = item["icon"]?.GetValue<string>() ?? string.Empty;
        var url = item["url"]?.GetValue<string>() ?? string.Empty;

        return new ListItem(new LinkCommand(url, Resources))
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

    protected override Task<IEnumerable<JsonObject>> LoadContentData()
    {
        var azureUri = new AzureUri(CurrentSearch.SearchString);
        var queryInfo = AzureDataManager!.GetQuery(azureUri, DeveloperId.LoginId);

        var queryResults = queryInfo is null
            ? new Dictionary<string, object>()
            : JsonConvert.DeserializeObject<Dictionary<string, object>>(queryInfo.QueryResults);

        var itemsArray = new JsonArray();

        foreach (var element in queryResults!)
        {
            var workItem = JsonNode.Parse(element.Value.ToStringInvariant());

            if (workItem != null)
            {
                var dateTicks = workItem["System.ChangedDate"]?.GetValue<long>() ?? DateTime.UtcNow.Ticks;
                var dateTime = dateTicks.ToDateTime();
                var creator = AzureDataManager.GetIdentity(workItem["System.CreatedBy"]?.GetValue<long>() ?? 0L);
                var workItemType = AzureDataManager.GetWorkItemType(workItem["System.WorkItemType"]?.GetValue<long>() ?? 0L);

                var item = new JsonObject
            {
                { "title", workItem["System.Title"]?.GetValue<string>() ?? string.Empty },
                { "url", workItem[AzureDataManager.WorkItemHtmlUrlFieldName]?.GetValue<string>() ?? string.Empty },
                { "icon", GetIconForType(workItemType.Name) },
                { "status_icon", GetIconForStatusState(workItem["System.State"]?.GetValue<string>()) },
                { "number", element.Key },
                { "date", TimeSpanHelper.DateTimeOffsetToDisplayString(dateTime, Log) },
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

        return Task.FromResult(itemsList.AsEnumerable());
    }
}
