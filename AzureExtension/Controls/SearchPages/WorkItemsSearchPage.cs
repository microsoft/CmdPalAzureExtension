// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls.Commands;
using AzureExtension.Controls.SearchPages;
using AzureExtension.DataManager;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Serilog;

namespace AzureExtension.Controls.Pages;

public sealed partial class WorkItemsSearchPage : SearchPage<IWorkItem>
{
    // Max number of query results to fetch for a given query.
    public static readonly int QueryResultLimit = 25;

    private readonly Lazy<ILogger> _log = new(() => Serilog.Log.ForContext("SourceContext", $"Pages/WorkItemsSearchPage"));

    private ILogger Log => _log.Value;

    private readonly IQuery _query;

    private readonly IResources _resources;

    private readonly IDataProvider _dataProvider;

    private readonly TimeSpanHelper _timeSpanHelper;

    public WorkItemsSearchPage(IQuery query, IResources resources, IDataProvider dataProvider, TimeSpanHelper timeSpanHelper)
        : base(query)
    {
        _query = query;
        _resources = resources;
        _dataProvider = dataProvider;
        Icon = new IconInfo(AzureIcon.IconDictionary["logo"]);
        Name = query.Name;
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
        else
        {
            return new IListItem[]
            {
                new ListItem(new NoOpCommand())
                {
                    Title = "No work items found for search",
                    Icon = new IconInfo(AzureIcon.IconDictionary["logo"]),
                },
            };
        }
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

    protected override ListItem GetListItem(IWorkItem item)
    {
        var title = item.SystemTitle;
        var url = item.HtmlUrl;

        return new ListItem(new LinkCommand(url, _resources))
        {
            Title = title,
            Icon = new IconInfo(AzureIcon.IconDictionary["logo"]),
        };
    }

    protected override Task<IEnumerable<IWorkItem>> LoadContentData()
    {
        return _dataProvider.GetWorkItems(_query);
    }
}
