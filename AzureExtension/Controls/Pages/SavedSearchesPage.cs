// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls;
using AzureExtension.Controls.Pages;
using AzureExtension.DeveloperId;
using AzureExtension.Helpers;
using AzureExtension.PersistentData;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension;

public partial class SavedSearchesPage : ListPage
{
    private readonly IListItem _addSearchListItem;

    private readonly IResources _resources;

    private readonly SavedSearchesMediator _savedSearchesMediator;

    private readonly PersistentDataManager _searchRepository;

    private readonly ISearchPageFactory _searchPageFactory;

    private IDeveloperIdProvider? _developerIdProvider;

    private AzureDataManager _azureDataManager;

    public SavedSearchesPage(
       IResources resources,
       IListItem addSearchListItem,
       SavedSearchesMediator savedSearchesMediator,
       IDeveloperIdProvider developerIdProvider,
       AzureDataManager azureDataManager,
       PersistentDataManager searchRepository,
       ISearchPageFactory searchPageFactory)
    {
        _resources = resources;

        Icon = new IconInfo("\ue721");
        Name = _resources.GetResource("Pages_Saved_Searches");
        _savedSearchesMediator = savedSearchesMediator;
        _savedSearchesMediator.SearchRemoved += OnSearchRemoved;
        _savedSearchesMediator.SearchRemoving += OnSearchRemoving;
        _addSearchListItem = addSearchListItem;
        _savedSearchesMediator.SearchSaved += OnSearchSaved;
        _developerIdProvider = developerIdProvider;
        _azureDataManager = azureDataManager;
        _searchRepository = searchRepository;
        _searchPageFactory = searchPageFactory;
    }

    private void OnSearchRemoved(object? sender, object? args)
    {
        IsLoading = false;

        if (args is Exception e)
        {
            var toast = new ToastStatusMessage(new StatusMessage()
            {
                Message = $"{_resources.GetResource("Pages_Saved_Searches_Error")} {e.Message}",
                State = MessageState.Error,
            });

            toast.Show();
        }
        else if (args is true)
        {
            RaiseItemsChanged(0);
        }
        else if (args is false)
        {
            var toast = new ToastStatusMessage(new StatusMessage()
            {
                Message = _resources.GetResource("Pages_Saved_Searches_Failure"),
                State = MessageState.Error,
            });

            toast.Show();
        }
    }

    private void OnSearchRemoving(object? sender, object? e)
    {
        IsLoading = true;
    }

    public override IListItem[] GetItems()
    {
        var savedSearches = _searchRepository.GetSavedSearches().Result;
        if (savedSearches.Any())
        {
            var searchPages = savedSearches.Select(savedSearch => _searchPageFactory.CreateItemForSearch(savedSearch)).ToList();

            searchPages.Add(_addSearchListItem);

            return searchPages.ToArray();
        }
        else
        {
            return [_addSearchListItem];
        }
    }

    public void OnSearchSaved(object? sender, object? args)
    {
        IsLoading = false;

        if (args != null && args is SearchCandidate searchCandidate)
        {
            RaiseItemsChanged(0);
        }

        // errors are handled in SaveSearchPage
    }
}
