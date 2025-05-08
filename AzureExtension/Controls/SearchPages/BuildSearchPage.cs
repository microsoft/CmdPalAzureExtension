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

public partial class BuildSearchPage : ListPage
{
    protected ILogger Logger { get; }

    public IDataProvider DataProvider { get; private set; }

    private readonly IDefinitionSearch _search;

    private readonly IDefinition _definition;

    private readonly IResources _resources;

    private readonly IDataProvider _dataProvider;

    private readonly TimeSpanHelper _timeSpanHelper;

    public BuildSearchPage(IDefinitionSearch search, IResources resources, IDataProvider dataProvider, TimeSpanHelper timeSpanHelper)
    {
        _search = search;
        _resources = resources;
        _dataProvider = dataProvider;
        _timeSpanHelper = timeSpanHelper;
        _definition = GetDefinitionForPage(_search).Result;
        Icon = GetIcon();
        Name = _definition.Name ?? $"Definition #{_definition.InternalId}";
        ShowDetails = true;
        Logger = Log.ForContext("SourceContext", $"Pages/{GetType().Name}");
        DataProvider = dataProvider;
    }

    private async Task<IDefinition> GetDefinitionForPage(IDefinitionSearch search)
    {
        var definition = await _dataProvider.GetDefinition(search);
        if (definition == null)
        {
            throw new InvalidOperationException($"Definition not found for search {search.InternalId} - {search.ProjectUrl}");
        }

        return definition;
    }

    private IconInfo GetIcon()
    {
        var builds = _dataProvider.GetBuilds(_search).GetAwaiter().GetResult();
        if (builds != null && builds.Any())
        {
            return IconLoader.GetIconForPipelineStatusAndResult(builds.First().Status, builds.First().Result);
        }

        return IconLoader.GetIcon("Logo");
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

    private async Task<IEnumerable<IBuild>> GetSearchItemsAsync()
    {
        DataProvider.OnUpdate += CacheManagerUpdateHandler;

        var items = await LoadContentData();

        Logger.Information($"Found {items.Count()} items matching search query \"{_search.InternalId}\"");

        return items;
    }

    protected ListItem GetListItem(IBuild item)
    {
        var title = $"#{item.BuildNumber}";

        return new ListItem(new LinkCommand(item.Url, _resources, null))
        {
            Title = title,
            Icon = IconLoader.GetIconForPipelineStatusAndResult(item.Status, item.Result),
            Tags = new ITag[]
            {
                new Tag(_timeSpanHelper.DateTimeOffsetToDisplayString(new DateTime(item.StartTime), null)),
            },
            Details = new Details()
            {
                Title = $"{_definition.Name} - {title}",
                Metadata = new[]
                {
                    new DetailsElement()
                    {
                        Key = "Requester",
                        Data = new DetailsLink() { Text = $"{item.Requester?.Name}" },
                    },
                    new DetailsElement()
                    {
                        Key = "Source Branch",
                        Data = new DetailsLink() { Text = $"{item.SourceBranch}" },
                    },
                },
            },
        };
    }

    protected Task<IEnumerable<IBuild>> LoadContentData()
    {
        return _dataProvider.GetBuilds(_search);
    }
}
