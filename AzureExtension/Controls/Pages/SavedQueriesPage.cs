// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls;
using AzureExtension.Controls.Pages;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension;

public partial class SavedQueriesPage : SavedSearchesPage
{
    private readonly IListItem _addQueryListItem;
    private readonly IResources _resources;
    private readonly ISavedSearchesProvider<IQuerySearch> _queryRepository;
    private readonly ISearchPageFactory _searchPageFactory;

    protected override SearchUpdatedType SearchUpdatedType => SearchUpdatedType.Query;

    protected override string ExceptionMessage => _resources.GetResource("Pages_SavedQueries_Error");

    public SavedQueriesPage(
       IResources resources,
       IListItem addQueryListItem,
       SavedAzureSearchesMediator mediator,
       ISavedSearchesProvider<IQuerySearch> queryRepository,
       ISearchPageFactory searchPageFactory)
        : base(mediator)
    {
        _resources = resources;
        Title = _resources.GetResource("Pages_SavedQueries");
        Name = Title; // Title is for the Page, Name is for the command
        Icon = IconLoader.GetIcon("QueryList");
        _addQueryListItem = addQueryListItem;
        _queryRepository = queryRepository;
        _searchPageFactory = searchPageFactory;
    }

    public override IListItem[] GetItems()
    {
        var searches = _queryRepository.GetSavedSearches(false);

        if (searches.Any())
        {
            var searchPages = searches.Select(savedSearch => _searchPageFactory.CreateItemForSearch(savedSearch)).ToList();

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

        if (args != null && args is IQuerySearch)
        {
            RaiseItemsChanged(0);
        }

        // errors are handled in SaveQueryPage
    }
}
