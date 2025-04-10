// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Client;
using AzureExtension.Controls;
using AzureExtension.Controls.Commands;
using AzureExtension.Controls.Forms;
using AzureExtension.Controls.Pages;
using AzureExtension.DataManager;
using AzureExtension.DeveloperId;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension;

public partial class SavedSearchesPage : ListPage
{
    private readonly IListItem _addSearchListItem;
    private readonly IResources _resources;
    private readonly SavedSearchesMediator _savedSearchesMediator;
    private readonly List<Query> _searches = new List<Query>();
    private readonly TimeSpanHelper _timeSpanHelper;
    private readonly IDataProvider _dataProvider;
    private readonly IAccountProvider _accountProvider;
    private readonly AzureClientHelpers _azureClientHelpers;

    public SavedSearchesPage(
       IResources resources,
       IListItem addSearchListItem,
       SavedSearchesMediator savedSearchesMediator,
       IDataProvider dataProvider,
       IAccountProvider accountProvider,
       AzureClientHelpers azureClientHelpers,
       TimeSpanHelper timeSpanHelper)
    {
        _resources = resources;

        Icon = new IconInfo("\ue721");
        Name = _resources.GetResource("Pages_Saved_Searches");
        _savedSearchesMediator = savedSearchesMediator;
        _savedSearchesMediator.SearchRemoved += OnSearchRemoved;
        _savedSearchesMediator.SearchRemoving += OnSearchRemoving;
        _addSearchListItem = addSearchListItem;
        _savedSearchesMediator.SearchSaved += OnSearchSaved;
        _timeSpanHelper = timeSpanHelper;
        _dataProvider = dataProvider;
        _accountProvider = accountProvider;
        _azureClientHelpers = azureClientHelpers;
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
        else if (args != null && args is Query query)
        {
            _searches.Remove(query);
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

    private void OnSearchRemoving(object? sender, object? e)
    {
        IsLoading = true;
    }

    public override IListItem[] GetItems()
    {
        if (_searches.Count != 0)
        {
            var searchPages = _searches.Select(savedSearch => CreateItemForSearch(savedSearch)).ToList();

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

        if (args != null && args is Query query)
        {
            _searches.Add(query);
            RaiseItemsChanged(0);
        }

        // errors are handled in SaveSearchPage
    }

    public IListItem CreateItemForSearch(Query search)
    {
        return new ListItem(CreatePageForSearch(search))
        {
            Title = search.Name,
            Subtitle = search.AzureUri.ToString(),
            Icon = new IconInfo(AzureIcon.IconDictionary[$"logo"]),
            MoreCommands = new CommandContextItem[]
            {
                new(new LinkCommand(search.AzureUri.ToString(), _resources)),
                new(new RemoveSavedSearchCommand(search, _resources, _savedSearchesMediator)),
                new(new EditSearchPage(
                    _resources,
                    new SaveSearchForm(
                        search,
                        _resources,
                        _savedSearchesMediator,
                        _accountProvider,
                        _azureClientHelpers),
                    new StatusMessage(),
                    _resources.GetResource("Pages_Search_Edited_Success"),
                    _resources.GetResource("Pages_Search_Edited_Failed"))),
            },
        };
    }

    private ListPage CreatePageForSearch(Query search)
    {
        return new WorkItemsSearchPage(search, _resources, _dataProvider, _timeSpanHelper)
        {
            Icon = new IconInfo(AzureIcon.IconDictionary["logo"]),
            Name = search.Name,
            IsLoading = true,
        };
    }
}
