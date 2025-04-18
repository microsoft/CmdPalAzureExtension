// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Account;
using AzureExtension.Client;
using AzureExtension.Controls;
using AzureExtension.Controls.Commands;
using AzureExtension.Controls.Forms;
using AzureExtension.Controls.Pages;
using AzureExtension.DataManager;
using AzureExtension.Helpers;
using AzureExtension.PersistentData;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Query = AzureExtension.Controls.Query;

namespace AzureExtension;

public partial class SavedQueriesPage : ListPage
{
    private readonly IListItem _addQueryListItem;
    private readonly IResources _resources;
    private readonly SavedAzureSearchesMediator _savedQueriesMediator;
    private readonly TimeSpanHelper _timeSpanHelper;
    private readonly IDataProvider _dataProvider;
    private readonly IAccountProvider _accountProvider;
    private readonly AzureClientHelpers _azureClientHelpers;
    private readonly IQueryRepository _queryRepository;
    private readonly ISearchPageFactory _searchPageFactory;

    public SavedQueriesPage(
       IResources resources,
       IListItem addQueryListItem,
       SavedAzureSearchesMediator savedQueriesMediator,
       IDataProvider dataProvider,
       IAccountProvider accountProvider,
       AzureClientHelpers azureClientHelpers,
       IQueryRepository queryRepository,
       TimeSpanHelper timeSpanHelper,
       ISearchPageFactory searchPageFactory)
    {
        _resources = resources;

        Icon = new IconInfo("\ue721");
        Name = _resources.GetResource("Pages_Saved_Searches");
        _savedQueriesMediator = savedQueriesMediator;
        _savedQueriesMediator.QueryRemoved += OnQueryRemoved;
        _savedQueriesMediator.QueryRemoving += OnQueryRemoving;
        _addQueryListItem = addQueryListItem;
        _savedQueriesMediator.QuerySaved += OnQuerySaved;
        _timeSpanHelper = timeSpanHelper;
        _dataProvider = dataProvider;
        _accountProvider = accountProvider;
        _azureClientHelpers = azureClientHelpers;
        _queryRepository = queryRepository;
        _searchPageFactory = searchPageFactory;
    }

    private void OnQueryRemoved(object? sender, object? args)
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
        else if (args != null && args is IQuery query)
        {
            RaiseItemsChanged(0);

            // no toast yet
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

    private void OnQueryRemoving(object? sender, object? e)
    {
        IsLoading = true;
    }

    public override IListItem[] GetItems()
    {
        var searches = _queryRepository.GetSavedQueries().Result;

        if (searches.Any())
        {
            var searchPages = searches.Select(savedSearch => _searchPageFactory.CreateItemForSearch(savedSearch, _queryRepository)).ToList();

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
