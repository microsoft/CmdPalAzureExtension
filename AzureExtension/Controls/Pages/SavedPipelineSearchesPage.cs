// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Account;
using AzureExtension.Controls.ListItems;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Pages;

public class SavedPipelineSearchesPage : ListPage
{
    private readonly IResources _resources;

    private readonly AddPipelineSearchListItem _addPipelineSearchListItem;

    private readonly SavedAzureSearchesMediator _mediator;

    private readonly ISavedSearchesProvider<IDefinitionSearch> _definitionRepository;

    private readonly IAccountProvider _accountProvider;

    private readonly ISearchPageFactory _searchPageFactory;

    public SavedPipelineSearchesPage(
        IResources resources,
        AddPipelineSearchListItem addPipelineSearchListItem,
        SavedAzureSearchesMediator mediator,
        ISavedSearchesProvider<IDefinitionSearch> definitionRepository,
        IAccountProvider accountProvider,
        ISearchPageFactory searchPageFactory)
    {
        _resources = resources;
        Title = _resources.GetResource("Pages_SavedPipelineSearches_Title");
        Name = Title; // Title is for the Page, Name is for the command
        Icon = IconLoader.GetIcon("Pipeline");
        ShowDetails = true;
        _definitionRepository = definitionRepository;
        _addPipelineSearchListItem = addPipelineSearchListItem;
        _mediator = mediator;
        _mediator.PipelineSearchRemoved += OnPipelineSearchRemoved;
        _mediator.PipelineSearchRemoving += OnPipelineSearchRemoving;
        _mediator.PipelineSearchSaved += OnPipelineSearchSaved;
        _accountProvider = accountProvider;
        _searchPageFactory = searchPageFactory;
    }

    private void OnPipelineSearchRemoved(object? sender, object? args)
    {
        IsLoading = false;

        if (args is Exception e)
        {
            var toast = new ToastStatusMessage(new StatusMessage()
            {
                Message = $"{_resources.GetResource("Pages_SavedPipelineSearches_Error")} {e.Message}",
                State = MessageState.Error,
            });

            toast.Show();
        }
        else if (args != null && args is IDefinitionSearch search)
        {
            RaiseItemsChanged(0);

            // no toast yet
        }
        else if (args is false)
        {
            var toast = new ToastStatusMessage(new StatusMessage()
            {
                Message = _resources.GetResource("Pages_SavedPipelineSearches_Failure"),
                State = MessageState.Error,
            });

            toast.Show();
        }
    }

    private void OnPipelineSearchRemoving(object? sender, object? e)
    {
        IsLoading = true;
    }

    public override IListItem[] GetItems()
    {
        var account = _accountProvider.GetDefaultAccount();
        var searches = _definitionRepository.GetSavedSearches(false);

        if (searches.Any())
        {
            var searchPages = searches.Select(savedSearch => _searchPageFactory.CreateItemForSearch(savedSearch)).ToList();

            searchPages.Add(_addPipelineSearchListItem);

            return searchPages.ToArray();
        }
        else
        {
            return [_addPipelineSearchListItem];
        }
    }

    public void OnPipelineSearchSaved(object? sender, object? args)
    {
        IsLoading = false;

        if (args != null && args is IDefinitionSearch)
        {
            RaiseItemsChanged();
        }

        // errors are handled in SavePipelineSearchPage
    }
}
