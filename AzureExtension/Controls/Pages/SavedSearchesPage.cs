// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls;
using AzureExtension.Controls.Pages;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension;

public partial class SavedSearchesPage : ListPage
{
    private readonly IListItem _addSearchListItem;

    private readonly IResources _resources;

    private readonly SavedSearchesMediator _savedSearchesMediator;

    private readonly List<SearchPage<object>> _searchPages;

    public SavedSearchesPage(
       IResources resources,
       IListItem addSearchListItem,
       SavedSearchesMediator savedSearchesMediator)
    {
        _resources = resources;

        Icon = new IconInfo("\ue721");
        Name = _resources.GetResource("Pages_Saved_Searches");
        _savedSearchesMediator = savedSearchesMediator;
        _savedSearchesMediator.SearchRemoved += OnSearchRemoved;
        _savedSearchesMediator.SearchRemoving += OnSearchRemoving;
        _addSearchListItem = addSearchListItem;
        _savedSearchesMediator.SearchSaved += OnSearchSaved;
        _searchPages = new List<SearchPage<object>>();
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
        var items = _searchPages
            .Select(searchPage => searchPage.GetListItem(searchPage.CurrentSearch))
            .ToList();

        var iListItems = items
            .Select(item => item as IListItem)
            .ToList();
        iListItems.Add(_addSearchListItem);
        return iListItems.ToArray();
    }

    // Change this to public to facilitate tests. As the event handler is
    // listening to a static event, it is not possible to mock the event.
    public void OnSearchSaved(object? sender, object? args)
    {
        IsLoading = false;

        if (args != null && args is SearchCandidate searchCandidate)
        {
            _searchPages.Add(new SearchPage<object>(
                searchCandidate,
                _resources));
            RaiseItemsChanged(0);
        }

        // errors are handled in SaveSearchPage
    }
}
