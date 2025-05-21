// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Account;
using AzureExtension.Controls;
using AzureExtension.Controls.Pages;
using AzureExtension.DataManager;
using AzureExtension.DataManager.Cache;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension;

public partial class AzureExtensionCommandProvider : CommandProvider
{
    private readonly SignInPage _signInPage;
    private readonly SignOutPage _signOutPage;
    private readonly SavedQueriesPage _savedQueriesPage;
    private readonly IAccountProvider _accountProvider;
    private readonly IResources _resources;
    private readonly SavedPullRequestSearchesPage _savedPullRequestSearchesPage;
    private readonly ISearchPageFactory _searchPageFactory;
    private readonly SavedAzureSearchesMediator _savedSearchesMediator;
    private readonly AuthenticationMediator _authenticationMediator;
    private readonly SavedPipelineSearchesPage _savedPipelineSearchesPage;
    private readonly ILiveDataProvider _liveDataProvider;

    public AzureExtensionCommandProvider(
        SignInPage signInPage,
        SignOutPage signOutPage,
        IAccountProvider accountProvider,
        SavedQueriesPage savedQueriesPage,
        IResources resources,
        SavedPullRequestSearchesPage savedPullRequestSearchesPage,
        ISearchPageFactory searchPageFactory,
        SavedAzureSearchesMediator mediator,
        AuthenticationMediator authenticationMediator,
        SavedPipelineSearchesPage savedPipelineSearchesPage,
        ILiveDataProvider liveDataProvider)
    {
        _signInPage = signInPage;
        _signOutPage = signOutPage;
        _accountProvider = accountProvider;
        _savedQueriesPage = savedQueriesPage;
        _resources = resources;
        _savedPullRequestSearchesPage = savedPullRequestSearchesPage;
        _searchPageFactory = searchPageFactory;
        _savedSearchesMediator = mediator;
        _authenticationMediator = authenticationMediator;
        _savedPipelineSearchesPage = savedPipelineSearchesPage;
        _liveDataProvider = liveDataProvider;
        _liveDataProvider.OnUpdate.AddListener(OnLiveDataUpdate);
        DisplayName = "Azure Extension"; // hard-coded because it's a product title

        _savedSearchesMediator.SearchUpdated += OnSearchUpdated;
        _authenticationMediator.SignInAction += OnSignInStatusChanged;
        _authenticationMediator.SignOutAction += OnSignInStatusChanged;
    }

    private void OnLiveDataUpdate(object? source, CacheManagerUpdateEventArgs e)
    {
        if (e.Kind == CacheManagerUpdateKind.Updated && e.DataUpdateParameters != null)
        {
            if (e.DataUpdateParameters.UpdateType == DataUpdateType.All)
            {
                RaiseItemsChanged(0);
            }
        }
    }

    private void OnSignInStatusChanged(object? sender, SignInStatusChangedEventArgs e)
    {
        RaiseItemsChanged();
    }

    private void OnSearchUpdated(object? sender, SearchUpdatedEventArgs args)
    {
        if (args.Exception == null)
        {
            RaiseItemsChanged();
        }
    }

    public override ICommandItem[] TopLevelCommands()
    {
        if (!_accountProvider.IsSignedIn())
        {
            return new ICommandItem[]
            {
                new CommandItem(_signInPage)
                {
                    Subtitle = _resources.GetResource("Forms_SignIn_PageSubtitle"),
                },
            };
        }
        else
        {
            var topLevelCommands = GetTopLevelSearches().GetAwaiter().GetResult();
            var defaultCommands = new List<ListItem>
            {
                new(_savedQueriesPage),
                new(_savedPullRequestSearchesPage),
                new(_savedPipelineSearchesPage),
                new(_signOutPage)
                {
                   Subtitle = _resources.GetResource("Forms_SignOut_PageTitle"),
                },
            };

            topLevelCommands.AddRange(defaultCommands);

            return topLevelCommands.ToArray();
        }
    }

    private async Task<List<IListItem>> GetTopLevelSearches()
    {
        return await _searchPageFactory.CreateCommandsForTopLevelSearches();
    }
}
