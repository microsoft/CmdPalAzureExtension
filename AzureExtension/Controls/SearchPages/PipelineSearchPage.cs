// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls.Commands;
using AzureExtension.DataManager.Cache;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Serilog;

namespace AzureExtension.Controls.Pages;

public partial class PipelineSearchPage : ListPage
{
    protected ILogger Logger { get; }

    public IDataProvider DataProvider { get; private set; }

    private readonly IDefinitionSearch _search;

    private readonly IResources _resources;

    private readonly IDataProvider _dataProvider;

    private readonly TimeSpanHelper _timeSpanHelper;

    public PipelineSearchPage(IDefinitionSearch search, IResources resources, IDataProvider dataProvider, TimeSpanHelper timeSpanHelper)
    {
        _search = search;
        _resources = resources;
        _dataProvider = dataProvider;
        _timeSpanHelper = timeSpanHelper;
        Icon = IconLoader.GetIcon("Pipeline");
        Name = search.InternalId.ToStringInvariant();
        ShowDetails = true;
        Logger = Log.ForContext("SourceContext", $"Pages/{GetType().Name}");
        DataProvider = dataProvider;
    }

    protected void CacheManagerUpdateHandler(object? source, CacheManagerUpdateEventArgs e)
    {
        if (e.Kind == CacheManagerUpdateKind.Updated)
        {
            Logger.Information($"Received cache manager update event.");
            RaiseItemsChanged(0);
        }
    }

    public override IListItem[] GetItems() => DoGetItems(SearchText).GetAwaiter().GetResult();

    private async Task<IListItem[]> DoGetItems(string searchText)
    {
        try
        {
            var items = await GetSearchItemsAsync();
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
                        Title = "No items found",
                        Icon = IconLoader.GetIcon("Logo"),
                    },
                };
            }
        }
        catch (Exception ex)
        {
            return new ListItem[]
            {
                new(new NoOpCommand())
                {
                    Title = "An error occurred with search",
                    Details = new Details()
                    {
                        Body = ex.Message,
                    },
                    Icon = new IconInfo(string.Empty),
                },
            };
        }
    }

    private async Task<IEnumerable<IDefinition>> GetSearchItemsAsync()
    {
        DataProvider.OnUpdate += CacheManagerUpdateHandler;

        var items = await LoadContentData();

        Logger.Information($"Found {items.Count()} items matching search query \"{_search.InternalId}\"");

        return items;
    }

    protected ListItem GetListItem(IDefinition item)
    {
        var title = item.Name;
        var url = item.InternalId.ToStringInvariant();

        return new ListItem(new LinkCommand(url, _resources))
        {
            Title = title,
            Icon = IconLoader.GetIcon("Logo"),
        };
    }

    protected Task<IEnumerable<IDefinition>> LoadContentData()
    {
        return (Task<IEnumerable<IDefinition>>)_dataProvider.GetDefinition(_search).Result;
    }
}
