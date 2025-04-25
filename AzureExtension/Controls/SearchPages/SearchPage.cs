// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.DataManager.Cache;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Serilog;

namespace AzureExtension.Controls.Pages;

public abstract partial class SearchPage<T> : ListPage
{
    protected ILogger Logger { get; }

    public IAzureSearch CurrentSearch { get; private set; }

    public IDataProvider DataProvider { get; private set; }

    public SearchPage(IAzureSearch search, IDataProvider dataProvider)
    {
        CurrentSearch = search;
        Icon = AzureIcon.IconDictionary["logo"];
        Name = search.Name;
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
                        Icon = AzureIcon.IconDictionary["logo"],
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

    private async Task<IEnumerable<T>> GetSearchItemsAsync()
    {
        DataProvider.OnUpdate += CacheManagerUpdateHandler;

        var items = await LoadContentData();

        Logger.Information($"Found {items.Count()} items matching search query \"{CurrentSearch.Name}\"");

        return items;
    }

    protected abstract ListItem GetListItem(T item);

    protected abstract Task<IEnumerable<T>> LoadContentData();
}
