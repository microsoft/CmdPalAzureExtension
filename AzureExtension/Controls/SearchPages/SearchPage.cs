// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.SearchPages;

public abstract partial class SearchPage<T> : ListPage
{
    public IAzureSearch CurrentSearch { get; private set; }

    public SearchPage(IAzureSearch search)
    {
        CurrentSearch = search;
        Icon = new IconInfo(AzureIcon.IconDictionary["logo"]);
        Name = search.Name;
    }

    public override IListItem[] GetItems() => DoGetItems(SearchText).GetAwaiter().GetResult();

    private async Task<IListItem[]> DoGetItems(string searchText)
    {
        try
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

    protected abstract ListItem GetListItem(T item);

    protected abstract Task<IEnumerable<T>> LoadContentData();
}
