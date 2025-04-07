// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls.Forms;
using AzureExtension.DeveloperId;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Pages;

public class SearchPageFactory : ISearchPageFactory
{
    private readonly IResources _resources;
    private readonly SavedSearchesMediator _savedSearchesMediator;
    private readonly IDeveloperIdProvider _developerIdProvider;
    private readonly AzureDataManager _azureDataManager;

    public SearchPageFactory(IResources resources, SavedSearchesMediator savedSearchesMediator, IDeveloperIdProvider developerIdProvider, AzureDataManager azureDataManager)
    {
        _resources = resources;
        _savedSearchesMediator = savedSearchesMediator;
        _developerIdProvider = developerIdProvider;
        _azureDataManager = azureDataManager;
    }

    private ListPage CreatePageForSearch(ISearch search)
    {
        return new SearchPage<object>(search, _resources, _developerIdProvider, _azureDataManager);
    }

    public IListItem CreateItemForSearch(ISearch search)
    {
        return new ListItem(CreatePageForSearch(search))
        {
            Title = search.Name,
            Subtitle = search.SearchString,
            Icon = new IconInfo(AzureIcon.IconDictionary["logo"]),
            MoreCommands = new CommandContextItem[]
            {
                new(new EditSearchPage(
                    _resources,
                    new SaveSearchForm(search, _resources, _savedSearchesMediator, _developerIdProvider),
                    new StatusMessage(),
                    _resources.GetResource("Pages_Search_Edited_Success"),
                    _resources.GetResource("Pages_Search_Edited_Failed"))),
            },
        };
    }
}
