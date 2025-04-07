// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Serilog;

namespace AzureExtension.Controls.Pages;

public class SearchPage<T> : ListPage
{
    protected ILogger Logger { get; }

    public ISearch CurrentSearch { get; private set; }

    protected IResources Resources { get; private set; }

    // Search is mandatory for this page to exist
    public SearchPage(ISearch search, IResources resources)
    {
        Icon = new IconInfo(AzureIcon.IconDictionary["logo"]);
        Name = search.Name;
        CurrentSearch = search;
        Logger = Log.ForContext("SourceContext", $"Pages/{GetType().Name}");
        Resources = resources;
    }

    public override IListItem[] GetItems() => DoGetItems(SearchText);

    private IListItem[] DoGetItems(string query)
    {
        try
        {
            Logger.Information($"Getting items for search query \"{CurrentSearch.Name}\"");
            var items = GetSearchItems();

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

    private ListItem GetListItem(object item) => new ListItem(new NoOpCommand())
    {
        Title = "Dummy Title",
        Icon = new IconInfo(AzureIcon.IconDictionary["logo"]),
    };

    private IEnumerable<T> GetSearchItems()
    {
        return Enumerable.Empty<T>();
    }
}
