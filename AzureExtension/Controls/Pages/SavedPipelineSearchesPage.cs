// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Account;
using AzureExtension.Controls.ListItems;
using AzureExtension.DataManager;
using AzureExtension.DataManager.Cache;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;

namespace AzureExtension.Controls.Pages;

public class SavedPipelineSearchesPage : SavedSearchesPage
{
    private readonly IResources _resources;
    private readonly AddPipelineSearchListItem _addPipelineSearchListItem;
    private readonly ISavedSearchesProvider<IPipelineDefinitionSearch> _definitionRepository;
    private readonly IAccountProvider _accountProvider;
    private readonly ISearchPageFactory _searchPageFactory;
    private readonly ILiveContentDataProvider<IBuild> _buildProvider;

    protected override SearchUpdatedType SearchUpdatedType => SearchUpdatedType.Pipeline;

    protected override string ExceptionMessage => _resources.GetResource("Pages_SavedPipelineSearches_Error");

    public SavedPipelineSearchesPage(
        IResources resources,
        AddPipelineSearchListItem addPipelineSearchListItem,
        SavedAzureSearchesMediator mediator,
        ISavedSearchesProvider<IPipelineDefinitionSearch> definitionRepository,
        IAccountProvider accountProvider,
        ILiveContentDataProvider<IBuild> buildProvider,
        ISearchPageFactory searchPageFactory)
        : base(mediator)
    {
        _resources = resources;
        Title = _resources.GetResource("Pages_SavedPipelineSearches_Title");
        Name = Title; // Title is for the Page, Name is for the command
        Icon = IconLoader.GetIcon("Pipeline");
        ShowDetails = true;
        _definitionRepository = definitionRepository;
        _addPipelineSearchListItem = addPipelineSearchListItem;
        _accountProvider = accountProvider;
        _searchPageFactory = searchPageFactory;
        _buildProvider = buildProvider;
        _buildProvider.OnUpdate.AddListener(OnUpdate);
    }

    private void OnUpdate(object? source, CacheManagerUpdateEventArgs e)
    {
        if (e.Kind == CacheManagerUpdateKind.Updated && e.DataUpdateParameters != null)
        {
            if (e.DataUpdateParameters.UpdateType == DataUpdateType.All)
            {
                RaiseItemsChanged(0);
            }
        }
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
}
