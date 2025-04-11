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
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension;

public partial class SavedQueriesPage : ListPage
{
    private readonly IListItem _addQueryListItem;
    private readonly IResources _resources;
    private readonly IDataProvider _dataProvider;
    private readonly IAccountProvider _accountProvider;
    private readonly AzureClientHelpers _azureClientHelpers;

    private readonly SavedQueriesMediator _savedQueriesMediator;

    private readonly List<Query> _queryes = new List<Query>();

    private readonly TimeSpanHelper _timeSpanHelper;

    public SavedQueriesPage(
       IResources resources,
       IListItem addQueryListItem,
       SavedQueriesMediator savedQueriesMediator,
       IDataProvider dataProvider,
       TimeSpanHelper timeSpanHelper,
       IAccountProvider accountProvider,
       AzureClientHelpers azureClientHelpers)
    {
        _resources = resources;

        Icon = new IconInfo("\ue721");
        Name = _resources.GetResource("Pages_Saved_Queries");
        _savedQueriesMediator = savedQueriesMediator;
        _savedQueriesMediator.QueryRemoved += OnQueryRemoved;
        _savedQueriesMediator.QueryRemoving += OnQueryRemoving;
        _addQueryListItem = addQueryListItem;
        _savedQueriesMediator.QuerySaved += OnQuerySaved;
        _dataProvider = dataProvider;
        _timeSpanHelper = timeSpanHelper;
        _accountProvider = accountProvider;
        _azureClientHelpers = azureClientHelpers;
    }

    private void OnQueryRemoved(object? sender, object? args)
    {
        IsLoading = false;

        if (args is Exception e)
        {
            var toast = new ToastStatusMessage(new StatusMessage()
            {
                Message = $"{_resources.GetResource("Pages_Saved_Queries_Error")} {e.Message}",
                State = MessageState.Error,
            });

            toast.Show();
        }
        else if (args != null && args is Query query)
        {
            _queryes.Remove(query);
            RaiseItemsChanged(0);

            // no toast yet
        }
        else if (args is false)
        {
            var toast = new ToastStatusMessage(new StatusMessage()
            {
                Message = _resources.GetResource("Pages_Saved_Queries_Failure"),
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
        if (_queryes.Count != 0)
        {
            var queryPages = _queryes.Select(savedQuery => CreateItemForQuery(savedQuery)).ToList();

            queryPages.Add(_addQueryListItem);

            return queryPages.ToArray();
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
            _queryes.Add(query);
            RaiseItemsChanged(0);
        }

        // errors are handled in SaveQueryPage
    }

    public IListItem CreateItemForQuery(Query query)
    {
        return new ListItem(CreatePageForQuery(query))
        {
            Title = query.Name,
            Subtitle = query.AzureUri.ToString(),
            Icon = new IconInfo(AzureIcon.IconDictionary[$"logo"]),
            MoreCommands = new CommandContextItem[]
            {
                new(new LinkCommand(query.AzureUri.ToString(), _resources)),
                new(new RemoveQueryCommand(query, _resources, _savedQueriesMediator)),
                new(new EditQueryPage(
                    _resources,
                    new SaveQueryForm(
                        query,
                        _resources,
                        _savedQueriesMediator,
                        _accountProvider,
                        _azureClientHelpers),
                    new StatusMessage(),
                    _resources.GetResource("Pages_Query_Edited_Success"),
                    _resources.GetResource("Pages_Query_Edited_Failed"))),
            },
        };
    }

    private ListPage CreatePageForQuery(Query query)
    {
        return new WorkItemsSearchPage(query, _resources, _dataProvider, _timeSpanHelper)
        {
            Icon = new IconInfo(AzureIcon.IconDictionary["logo"]),
            Name = query.Name,
            IsLoading = true,
        };
    }
}
