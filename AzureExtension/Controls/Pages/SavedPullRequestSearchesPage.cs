// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls.ListItems;
using AzureExtension.Helpers;
using AzureExtension.PersistentData;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Pages;

public class SavedPullRequestSearchesPage : ListPage
{
    private readonly IResources _resources;
    private readonly AddPullRequestSearchListItem _addPullRequestSearchListItem;
    private readonly SavedAzureSearchesMediator _mediator;
    private readonly ISavedPullRequestSearchRepository _pullRequestSearchRepository;
    private readonly ISearchPageFactory _searchPageFactory;

    public SavedPullRequestSearchesPage(
        IResources resources,
        AddPullRequestSearchListItem addPullRequestSearchListItem,
        SavedAzureSearchesMediator mediator,
        ISavedPullRequestSearchRepository pullRequestSearchRepository,
        ISearchPageFactory searchPageFactory)
    {
        _resources = resources;
        Title = _resources.GetResource("Pages_SavedPullRequestSearches_Title");
        Name = _resources.GetResource("Pages_SavedPullRequestSearches_Title"); // Title is for the Page, Name is for the command
        Icon = IconLoader.GetIcon("PullRequest");
        _pullRequestSearchRepository = pullRequestSearchRepository;
        _addPullRequestSearchListItem = addPullRequestSearchListItem;
        _mediator = mediator;
        _mediator.PullRequestSearchRemoved += OnPullRequestSearchRemoved;
        _mediator.PullRequestSearchRemoving += OnPullRequestSearchRemoving;
        _mediator.PullRequestSearchSaved += OnPullRequestSearchSaved;
        _searchPageFactory = searchPageFactory;
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
        else if (args != null && args is IPullRequestSearch search)
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

    private void OnPullRequestSearchRemoving(object? sender, object? e)
    {
        IsLoading = true;
    }

    public override IListItem[] GetItems()
    {
        var searches = _pullRequestSearchRepository.GetSavedPullRequestSearches().Result;

        if (searches.Any())
        {
            var searchPages = searches.Select(savedSearch => _searchPageFactory.CreateItemForSearch(savedSearch, _pullRequestSearchRepository)).ToList();

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
}
