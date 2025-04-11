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
    private readonly SavedQueriesMediator _savedQueriesMediator;
    private readonly TimeSpanHelper _timeSpanHelper;
    private readonly IDataProvider _dataProvider;
    private readonly IAccountProvider _accountProvider;
    private readonly AzureClientHelpers _azureClientHelpers;
    private readonly IQueryRepository _queryRepository;

    public SavedQueriesPage(
       IResources resources,
       IListItem addQueryListItem,
       SavedQueriesMediator savedQueriesMediator,
       IDataProvider dataProvider,
       IAccountProvider accountProvider,
       AzureClientHelpers azureClientHelpers,
       IQueryRepository queryRepository,
       TimeSpanHelper timeSpanHelper)
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
        else if (args != null && args is Query query)
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
            var searchPages = searches.Select(savedSearch => CreateItemForQuery(savedSearch)).ToList();

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

    public IListItem CreateItemForQuery(IQuery query)
    {
        return new ListItem(CreatePageForQuery(query))
        {
            Title = query.Name,
            Subtitle = query.Url,
            Icon = new IconInfo(AzureIcon.IconDictionary[$"logo"]),
            MoreCommands = new CommandContextItem[]
            {
                new(new LinkCommand(query.Url, _resources)),
                new(new RemoveQueryCommand(query, _resources, _savedQueriesMediator, _queryRepository)),
                new(new EditQueryPage(
                    _resources,
                    new SaveQueryForm(
                        query,
                        _resources,
                        _savedQueriesMediator,
                        _accountProvider,
                        _azureClientHelpers,
                        _queryRepository),
                    new StatusMessage(),
                    _resources.GetResource("Pages_Search_Edited_Success"),
                    _resources.GetResource("Pages_Search_Edited_Failed"))),
            },
        };
    }

    private ListPage CreatePageForQuery(IQuery search)
    {
        return new WorkItemsSearchPage(search, _resources, _dataProvider, _timeSpanHelper)
        {
            Icon = new IconInfo(AzureIcon.IconDictionary["logo"]),
            Name = search.Name,
            IsLoading = true,
        };
    }
}
