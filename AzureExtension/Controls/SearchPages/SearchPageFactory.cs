// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls.Commands;
using AzureExtension.DataManager;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Pages;

public class SearchPageFactory : ISearchPageFactory
{
    private readonly IResources _resources;

    private readonly IDataProvider _dataProvider;

    public SearchPageFactory(IResources resources, IDataProvider dataProvider)
    {
        _resources = resources;
        _dataProvider = dataProvider;
    }

    public ListPage CreatePageForSearch(IAzureSearch search)
    {
        return search.Type switch
        {
            AzureSearchType.Query => new WorkItemsSearchPage((IQuery)search, _resources, _dataProvider),
            AzureSearchType.PullRequestSearch => new PullRequestSearchPage((IPullRequestSearch)search, _resources, _dataProvider),
            _ => throw new NotImplementedException($"No page for search type {search.Type}"),
        };
    }

    public IListItem CreateItemForSearch(IAzureSearch search)
    {
        return new ListItem(CreatePageForSearch(search))
        {
            Title = search.Name,
            Subtitle = search.Url,
            Icon = new IconInfo(AzureIcon.IconDictionary["logo"]),
            MoreCommands = new CommandContextItem[]
            {
                new(new LinkCommand(search.Url, _resources))
                {
                    Title = search.Name,
                    Icon = new IconInfo(AzureIcon.IconDictionary["logo"]),
                },
            },
        };
    }
}
