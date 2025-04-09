// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.DataManager;
using AzureExtension.DeveloperId;
using AzureExtension.Helpers;
using AzureExtension.PersistentData;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Serilog;

namespace AzureExtension.Controls.Pages;

public abstract partial class SearchPage<T> : ListPage
{
    protected ILogger Logger { get; }

    public ISearch CurrentSearch { get; private set; }

    protected AzureDataManager AzureDataManager { get; private set; }

    protected PersistentDataManager PersistentDataManager { get; private set; }

    protected IResources Resources { get; private set; }

    protected IDeveloperId DeveloperId { get; private set; }

    // Search is mandatory for this page to exist
    protected SearchPage(ISearch search, AzureDataManager azureDataManager, PersistentDataManager persistentDataManager, IResources resources, IDeveloperId developerId)
    {
        Icon = new IconInfo(AzureIcon.IconDictionary["logo"]);
        Name = search.Name;
        CurrentSearch = search;
        Logger = Log.ForContext("SourceContext", $"Pages/{GetType().Name}");
        AzureDataManager = azureDataManager;
        PersistentDataManager = persistentDataManager;
        Resources = resources;
        DeveloperId = developerId;
    }

    public override IListItem[] GetItems() => DoGetItems(SearchText).GetAwaiter().GetResult();

    protected void CacheManagerUpdateHandler(object? source, CacheManagerUpdateEventArgs e)
    {
        if (e.Kind == CacheManagerUpdateKind.Updated)
        {
            Logger.Information($"Received cache manager update event.");
            RaiseItemsChanged(0);
        }
    }

    private async Task<IListItem[]> DoGetItems(string query)
    {
        try
        {
            Logger.Information($"Getting items for search query \"{CurrentSearch.Name}\"");
            var items = await GetSearchItemsAsync();

            var iconString = "logo";

            if (items.Any())
            {
                return items.Select(item => GetListItem(item)).ToArray();
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

    private async Task<IEnumerable<T>> GetSearchItemsAsync()
    {
        var items = await LoadContentData();

        Logger.Information($"Found {items.Count()} items matching search query \"{CurrentSearch.Name}\"");

        return items;
    }

    protected abstract ListItem GetListItem(T item);

    protected abstract Task<IEnumerable<T>> LoadContentData();
}
