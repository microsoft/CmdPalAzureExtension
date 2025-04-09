// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls;
using AzureExtension.Controls.Commands;
using AzureExtension.Controls.Forms;
using AzureExtension.Controls.Pages;
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

    private List<QueryObject> _searches = new List<QueryObject>();

    private IDeveloperIdProvider? _developerIdProvider;

    private AzureDataManager _azureDataManager;

    private TimeSpanHelper _timeSpanHelper;

    public SavedSearchesPage(
       IResources resources,
       IListItem addSearchListItem,
       SavedSearchesMediator savedSearchesMediator,
       IDeveloperIdProvider developerIdProvider,
       AzureDataManager azureDataManager,
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
        _developerIdProvider = developerIdProvider;
        _azureDataManager = azureDataManager;
        _timeSpanHelper = timeSpanHelper;
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
        else if (args != null && args is QueryObject queryObject)
        {
            _searches.Remove(queryObject);
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

        if (args != null && args is QueryObject queryObject)
        {
            _searches.Add(queryObject);
            RaiseItemsChanged(0);
        }

        // errors are handled in SaveSearchPage
    }

    public IListItem CreateItemForSearch(QueryObject search)
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
                        _developerIdProvider!),
                    new StatusMessage(),
                    _resources.GetResource("Pages_Search_Edited_Success"),
                    _resources.GetResource("Pages_Search_Edited_Failed"))),
            },
        };
    }

    private ListPage CreatePageForSearch(QueryObject search)
    {
        return new WorkItemsSearchPage(search, _developerIdProvider!.GetLoggedInDeveloperIds().DeveloperIds.FirstOrDefault()!, _resources, _azureDataManager, _timeSpanHelper)
        {
            Icon = new IconInfo(AzureIcon.IconDictionary["logo"]),
            Name = search.Name,
            IsLoading = true,
        };
    }
}
