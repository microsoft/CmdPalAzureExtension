// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Client;
using AzureExtension.Controls.Commands;
using AzureExtension.Controls.Forms;
using AzureExtension.Controls.ListItems;
using AzureExtension.Helpers;
using AzureExtension.PersistentData;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Pages;

public class SavedPullRequestSearchesPage : ListPage
{
    private readonly IResources _resources;

    private List<PullRequestSearch> _searches;

    private AddPullRequestSearchListItem _addPullRequestSearchListItem;

    private SavedQueriesMediator _mediator;

    public SavedPullRequestSearchesPage(IResources resources, AddPullRequestSearchListItem addPullRequestSearchListItem, SavedQueriesMediator mediator)
    {
        _resources = resources;
        _searches = new List<PullRequestSearch>();
        _addPullRequestSearchListItem = addPullRequestSearchListItem;
        _mediator = mediator;
        _mediator.PullRequestSearchRemoved += OnPullRequestSearchRemoved;
        _mediator.PullRequestSearchRemoving += OnPullRequestSearchRemoving;
        _mediator.PullRequestSearchSaved += OnPullRequestSearchSaved;
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
            _searches.Remove(search);
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
        if (_searches.Count > 0)
        {
            var searchPages = _searches.Select(savedSearch => CreateItemForPullRequestSearch(savedSearch)).ToList();

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
            _searches.Add(search);
            RaiseItemsChanged(0);
        }

        // errors are handled in SavePullRequestSearchPage
    }

    public IListItem CreateItemForPullRequestSearch(PullRequestSearch search)
    {
        return new ListItem(CreatePageForPullRequestSearch(search))
        {
            Title = search.Title,
            Subtitle = search.Url,
            Icon = new IconInfo(AzureIcon.IconDictionary[$"logo"]),
        };
    }

    private ListPage CreatePageForPullRequestSearch(PullRequestSearch search)
    {
        return new ListPage();
    }
}
