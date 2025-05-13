// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls.ListItems;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Pages;

public class SavedPullRequestSearchesPage : ListPage
{
    private readonly IResources _resources;
    private readonly AddPullRequestSearchListItem _addPullRequestSearchListItem;
    private readonly SavedAzureSearchesMediator _mediator;
    private readonly ISavedSearchesProvider<IPullRequestSearch> _pullRequestSearchRepository;
    private readonly ISearchPageFactory _searchPageFactory;

    public SavedPullRequestSearchesPage(
        IResources resources,
        AddPullRequestSearchListItem addPullRequestSearchListItem,
        SavedAzureSearchesMediator mediator,
        ISavedSearchesProvider<IPullRequestSearch> pullRequestSearchRepository,
        ISearchPageFactory searchPageFactory)
    {
        _resources = resources;
        Title = _resources.GetResource("Pages_SavedPullRequestSearches_Title");
        Name = _resources.GetResource("Pages_SavedPullRequestSearches_Title"); // Title is for the Page, Name is for the command
        Icon = IconLoader.GetIcon("PullRequest");
        _pullRequestSearchRepository = pullRequestSearchRepository;
        _addPullRequestSearchListItem = addPullRequestSearchListItem;
        _mediator = mediator;
        _searchPageFactory = searchPageFactory;
    }

    public void OnPullRequestSearchRemoved(object? sender, SearchUpdatedEventArgs args)
    {
        IsLoading = false;

        if (args.Exception != null)
        {
            var toast = new ToastStatusMessage(new StatusMessage()
            {
                Message = $"{_resources.GetResource("Pages_SavedPullRequestSearches_Error")} {args.Exception.Message}",
                State = MessageState.Error,
            });

            toast.Show();
        }
        else if (args.AzureSearch is IPullRequestSearch)
        {
            RaiseItemsChanged(0);

            // no toast yet
        }
        else if (!args.Success)
        {
            var toast = new ToastStatusMessage(new StatusMessage()
            {
                Message = _resources.GetResource("Pages_SavedPullRequestSearches_Failure"),
                State = MessageState.Error,
            });

            toast.Show();
        }
    }

    public void OnPullRequestSearchRemoving(object? sender, SearchUpdatedEventArgs args)
    {
        IsLoading = true;
    }

    public override IListItem[] GetItems()
    {
        var searches = _pullRequestSearchRepository.GetSavedSearches(false);

        if (searches.Any())
        {
            var searchPages = searches.Select(savedSearch => _searchPageFactory.CreateItemForSearch(savedSearch)).ToList();

            searchPages.Add(_addPullRequestSearchListItem);

            return searchPages.ToArray();
        }
        else
        {
            return [_addPullRequestSearchListItem];
        }
    }

    public void OnPullRequestSearchSaved(object? sender, SearchUpdatedEventArgs args)
    {
        IsLoading = false;

        if (args.AzureSearch is PullRequestSearchCandidate)
        {
            RaiseItemsChanged(0);
        }

        // errors are handled in SavePullRequestSearchPage
    }
}
