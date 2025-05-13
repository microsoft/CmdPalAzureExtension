// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls;
using AzureExtension.Controls.Pages;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Query = AzureExtension.Controls.Query;

namespace AzureExtension;

public partial class SavedQueriesPage : ListPage
{
    private readonly IListItem _addQueryListItem;
    private readonly IResources _resources;
    private readonly SavedAzureSearchesMediator _savedQueriesMediator;
    private readonly ISavedSearchesProvider<IQuerySearch> _queryRepository;
    private readonly ISearchPageFactory _searchPageFactory;

    public SavedQueriesPage(
       IResources resources,
       IListItem addQueryListItem,
       SavedAzureSearchesMediator savedQueriesMediator,
       ISavedSearchesProvider<IQuerySearch> queryRepository,
       ISearchPageFactory searchPageFactory)
    {
        _resources = resources;
        Title = _resources.GetResource("Pages_SavedQueries");
        Name = _resources.GetResource("Pages_SavedQueries"); // Title is for the Page, Name is for the command
        Icon = IconLoader.GetIcon("QueryList");
        _savedQueriesMediator = savedQueriesMediator;
        _addQueryListItem = addQueryListItem;
        _queryRepository = queryRepository;
        _searchPageFactory = searchPageFactory;
    }

    public void OnQueryRemoved(object? sender, SearchUpdatedEventArgs args)
    {
        IsLoading = false;

        if (args.Exception != null)
        {
            var toast = new ToastStatusMessage(new StatusMessage()
            {
                Message = $"{_resources.GetResource("Pages_SavedQueries_Error")} {args.Exception.Message}",
                State = MessageState.Error,
            });

            toast.Show();
        }
        else if (args.AzureSearch is IQuerySearch)
        {
            RaiseItemsChanged(0);

            // no toast yet
        }
        else if (!args.Success)
        {
            var toast = new ToastStatusMessage(new StatusMessage()
            {
                Message = _resources.GetResource("Pages_SavedQueries_Failure"),
                State = MessageState.Error,
            });

            toast.Show();
        }
    }

    public void OnQueryRemoving(object? sender, object? e)
    {
        IsLoading = true;
    }

    public override IListItem[] GetItems()
    {
        var searches = _queryRepository.GetSavedSearches(false);

        if (searches.Any())
        {
            var searchPages = searches.Select(savedSearch => _searchPageFactory.CreateItemForSearch(savedSearch)).ToList();

            searchPages.Add(_addQueryListItem);

            return searchPages.ToArray();
        }
        else
        {
            return [_addQueryListItem];
        }
    }

    public void OnQuerySaved(object? sender, object? args)
    {
        IsLoading = false;

        if (args != null && args is Query query)
        {
            RaiseItemsChanged(0);
        }

        // errors are handled in SaveQueryPage
    }
}
