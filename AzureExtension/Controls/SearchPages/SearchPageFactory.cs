// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls.Commands;
using AzureExtension.Controls.Forms;
using AzureExtension.DeveloperId;
using AzureExtension.Helpers;
using AzureExtension.PersistentData;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Pages;

public class SearchPageFactory : ISearchPageFactory
{
    private readonly PersistentDataManager _persistentDataManager;
    private readonly AzureDataManager _azureDataManager;
    private readonly IResources _resources;
    private readonly SavedSearchesMediator _savedSearchesMediator;
    private readonly IDeveloperIdProvider _developerIdProvider;
    private readonly TimeSpanHelper _timeSpanHelper;

    public SearchPageFactory(PersistentDataManager persistentDataManager, AzureDataManager azureDataManager, IResources resources, SavedSearchesMediator savedSearchesMediator, IDeveloperIdProvider developerIdProvider, TimeSpanHelper timeSpanHelper)
    {
        _persistentDataManager = persistentDataManager;
        _azureDataManager = azureDataManager;
        _resources = resources;
        _savedSearchesMediator = savedSearchesMediator;
        _developerIdProvider = developerIdProvider;
        _timeSpanHelper = timeSpanHelper;
    }

    private ListPage CreatePageForSearch(ISearch search)
    {
        return new WorkItemsSearchPage(search, _azureDataManager, _persistentDataManager, _resources, _developerIdProvider.GetLoggedInDeveloperIds().DeveloperIds.FirstOrDefault()!, _timeSpanHelper)
        {
            Icon = new IconInfo(AzureIcon.IconDictionary[$"logo"]),
            Name = search.Name,
        };
    }

    public IListItem CreateItemForSearch(ISearch search)
    {
        return new ListItem(CreatePageForSearch(search))
        {
            Title = search.Name,
            Subtitle = search.SearchString,
            Icon = new IconInfo(AzureIcon.IconDictionary[$"logo"]),
            MoreCommands = new CommandContextItem[]
            {
                new(new LinkCommand(search.SearchString, _resources)),
                new(new RemoveSavedSearchCommand(search, _persistentDataManager, _resources, _savedSearchesMediator, _developerIdProvider.GetLoggedInDeveloperIds().DeveloperIds.FirstOrDefault()!)),
                new(new EditSearchPage(
                    _resources,
                    new SaveSearchForm(
                        search,
                        _resources,
                        _savedSearchesMediator,
                        _developerIdProvider,
                        _persistentDataManager),
                    new StatusMessage(),
                    _resources.GetResource("Pages_Search_Edited_Success"),
                    _resources.GetResource("Pages_Search_Edited_Failed"))),
            },
        };
    }
}
