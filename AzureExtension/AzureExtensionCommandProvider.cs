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

public partial class AzureExtensionCommandProvider : CommandProvider, IDisposable
{
    private readonly SignInPage _signInPage;
    private readonly SignOutPage _signOutPage;
    private readonly SavedQueriesPage _savedQueriesPage;
    private readonly IAccountProvider _accountProvider;
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
        _savedPullRequestSearchesPage = savedPullRequestSearchesPage;
        _searchPageFactory = searchPageFactory;
        _savedSearchesMediator = mediator;
        _authenticationMediator = authenticationMediator;
        _savedPipelineSearchesPage = savedPipelineSearchesPage;
        _liveDataProvider = liveDataProvider;
        _liveDataProvider.OnUpdate += OnLiveDataUpdate;
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
                new CommandItem(_signInPage),
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
                new(_signOutPage),
            };

            topLevelCommands.AddRange(defaultCommands);

            return topLevelCommands.ToArray();
        }
    }

    private async Task<List<IListItem>> GetTopLevelSearches()
    {
        return await _searchPageFactory.CreateCommandsForTopLevelSearches();
    }

    // disposing area
    private bool _disposed;

    private void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (!disposing)
        {
            return;
        }

        _savedSearchesMediator.SearchUpdated -= OnSearchUpdated;
        _authenticationMediator.SignInAction -= OnSignInStatusChanged;
        _authenticationMediator.SignOutAction -= OnSignInStatusChanged;
        _liveDataProvider.OnUpdate -= OnLiveDataUpdate;

        _disposed = true;
    }

    void IDisposable.Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
