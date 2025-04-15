// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls.Commands;
using AzureExtension.Controls.ListItems;
using AzureExtension.DataManager;
using AzureExtension.Helpers;
using AzureExtension.PersistentData;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Pages;

public class SavedPullRequestSearchesPage : ListPage
{
    private readonly IResources _resources;
    private readonly AddPullRequestSearchListItem _addPullRequestSearchListItem;
    private readonly SavedQueriesMediator _mediator;
    private readonly IDataProvider _dataProvider;
    private readonly ISavedPullRequestSearchRepository _pullRequestSearchRepository;
    private readonly TimeSpanHelper _timeSpanHelper;

    public SavedPullRequestSearchesPage(
        IResources resources,
        AddPullRequestSearchListItem addPullRequestSearchListItem,
        SavedQueriesMediator mediator,
        IDataProvider dataProvider,
        ISavedPullRequestSearchRepository pullRequestSearchRepository,
        TimeSpanHelper timeSpanHelper)
    {
        _resources = resources;
        _pullRequestSearchRepository = pullRequestSearchRepository;
        _addPullRequestSearchListItem = addPullRequestSearchListItem;
        _mediator = mediator;
        _mediator.PullRequestSearchRemoved += OnPullRequestSearchRemoved;
        _mediator.PullRequestSearchRemoving += OnPullRequestSearchRemoving;
        _mediator.PullRequestSearchSaved += OnPullRequestSearchSaved;
        _dataProvider = dataProvider;
        _timeSpanHelper = timeSpanHelper;
    }

    private void OnPullRequestSearchRemoved(object? sender, object? args)
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
        else if (args != null && args is PullRequestSearch search)
        {
            _pullRequestSearchRepository.RemoveSavedPullRequestSearch(search).Wait();
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

    private void OnPullRequestSearchRemoving(object? sender, object? e)
    {
        IsLoading = true;
    }

    public override IListItem[] GetItems()
    {
        var searches = _pullRequestSearchRepository.GetSavedPullRequestSearches().Result;

        if (searches.Any())
        {
            var searchPages = searches.Select(savedSearch => CreateItemForPullRequestSearch(savedSearch)).ToList();

            searchPages.Add(_addPullRequestSearchListItem);

            return searchPages.ToArray();
        }
        else
        {
            return [_addPullRequestSearchListItem];
        }
    }

    public void OnPullRequestSearchSaved(object? sender, object? args)
    {
        IsLoading = false;

        if (args != null && args is PullRequestSearch search)
        {
            RaiseItemsChanged(0);
        }

        // errors are handled in SavePullRequestSearchPage
    }

    public IListItem CreateItemForPullRequestSearch(IPullRequestSearch search)
    {
        return new ListItem(CreatePageForPullRequestSearch(search))
        {
            Title = search.Title,
            Subtitle = search.Url,
            Icon = new IconInfo(AzureIcon.IconDictionary[$"logo"]),
            MoreCommands = new CommandContextItem[]
            {
                new(new LinkCommand(search.Url, _resources)),
            },
        };
    }

    private ListPage CreatePageForPullRequestSearch(IPullRequestSearch search)
    {
        return new PullRequestSearchPage(search, _resources, _dataProvider, _timeSpanHelper)
        {
            Icon = new IconInfo(AzureIcon.IconDictionary["logo"]),
            Name = search.Title,
        };
    }
}
