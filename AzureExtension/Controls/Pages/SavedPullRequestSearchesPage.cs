// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls.ListItems;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;

namespace AzureExtension.Controls.Pages;

public class SavedPullRequestSearchesPage : SavedSearchesPage
{
    private readonly IResources _resources;
    private readonly AddPullRequestSearchListItem _addPullRequestSearchListItem;
    private readonly ISavedSearchesProvider<IPullRequestSearch> _pullRequestSearchRepository;
    private readonly ISearchPageFactory _searchPageFactory;

    protected override SearchUpdatedType SearchUpdatedType => SearchUpdatedType.PullRequest;

    protected override string ExceptionMessage => _resources.GetResource("Pages_SavedPullRequestSearches_Error");

    public SavedPullRequestSearchesPage(
        IResources resources,
        AddPullRequestSearchListItem addPullRequestSearchListItem,
        SavedAzureSearchesMediator mediator,
        ISavedSearchesProvider<IPullRequestSearch> pullRequestSearchRepository,
        ISearchPageFactory searchPageFactory)
        : base(mediator)
    {
        _resources = resources;
        Title = _resources.GetResource("Pages_SavedPullRequestSearches_Title");
        Name = Title; // Title is for the Page, Name is for the command
        Icon = IconLoader.GetIcon("PullRequest");
        _pullRequestSearchRepository = pullRequestSearchRepository;
        _addPullRequestSearchListItem = addPullRequestSearchListItem;
        _searchPageFactory = searchPageFactory;
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
}
