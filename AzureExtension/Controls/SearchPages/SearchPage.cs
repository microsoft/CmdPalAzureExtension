// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.DataManager;
using AzureExtension.DataManager.Cache;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Serilog;

namespace AzureExtension.Controls.Pages;

public abstract partial class SearchPage<TContentData> : ListPage
    where TContentData : class
{
    protected ILogger Logger { get; }

    protected IAzureSearch CurrentSearch { get; private set; }

    private readonly ILiveContentDataProvider<TContentData> _contentDataProvider;
    private readonly IResources _resources;

    public SearchPage(IAzureSearch search, ILiveContentDataProvider<TContentData> dataProvider, IResources resources)
    {
        CurrentSearch = search;
        Name = search.Name;
        Logger = Log.ForContext("SourceContext", $"Pages/{GetType().Name}");
        _contentDataProvider = dataProvider;
        _contentDataProvider.WeakOnUpdate += OnCacheManagerUpdateHandler;
        _resources = resources;
    }

    public void OnCacheManagerUpdateHandler(object? source, CacheManagerUpdateEventArgs e)
    {
        if (e.Kind == CacheManagerUpdateKind.Updated && e.DataUpdateParameters != null)
        {
            // This should check if this is the search that originated the update.
            if (e.DataUpdateParameters.UpdateType == DataUpdateType.All || e.DataUpdateParameters.UpdateObject == CurrentSearch)
            {
                Logger.Information($"Received cache manager update event.");
                RaiseItemsChanged(0);
            }
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
                        Title = _resources.GetResource("Pages_Search_NoItemsFound"),
                        Icon = GetIconForSearch(CurrentSearch),
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
                    Title = _resources.GetResource("Pages_Search_ErrorMessage"),
                    Details = new Details()
                    {
                        Body = ex.Message,
                    },
                    Icon = IconLoader.GetIcon("Failure"),
                },
            };
        }
    }

    private async Task<IEnumerable<TContentData>> GetSearchItemsAsync()
    {
        var items = await LoadContentData();

        Logger.Information($"Found {items.Count()} items matching search query \"{CurrentSearch.Name}\"");

        return items;
    }

    protected abstract ListItem GetListItem(TContentData item);

    private Task<IEnumerable<TContentData>> LoadContentData()
    {
        return _contentDataProvider.GetContentData(CurrentSearch);
    }

    private IconInfo GetIconForSearch(IAzureSearch search)
    {
        if (search is IQuerySearch)
        {
            return IconLoader.GetIcon("Query");
        }
        else if (search is IPullRequestSearch)
        {
            return IconLoader.GetIcon("PullRequest");
        }
        else if (search is IPipelineDefinitionSearch)
        {
            return IconLoader.GetIcon("Pipeline");
        }
        else
        {
            return IconLoader.GetIcon("Logo");
        }
    }
}
