// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Account;
using AzureExtension.Controls;
using AzureExtension.Controls.Pages;
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

    public AzureExtensionCommandProvider(SignInPage signInPage, SignOutPage signOutPage, IAccountProvider accountProvider, SavedQueriesPage savedQueriesPage, IResources resources, SavedPullRequestSearchesPage savedPullRequestSearchesPage, ISearchPageFactory searchPageFactory, SavedAzureSearchesMediator mediator, AuthenticationMediator authenticationMediator)
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
        DisplayName = "Azure Extension";

        _savedSearchesMediator.QuerySaved += OnSearchUpdated;
        _savedSearchesMediator.QueryRemoved += OnSearchUpdated;
        _savedSearchesMediator.PullRequestSearchSaved += OnSearchUpdated;
        _savedSearchesMediator.PullRequestSearchRemoved += OnSearchUpdated;
        _authenticationMediator.SignInAction += OnSignInStatusChanged;
        _authenticationMediator.SignOutAction += OnSignInStatusChanged;
    }

    private void OnSignInStatusChanged(object? sender, SignInStatusChangedEventArgs e)
    {
        RaiseItemsChanged();
    }

    private void OnSearchUpdated(object? sender, object? args)
    {
        if (args is IQuery || args is IPullRequestSearch)
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
                    Icon = new IconInfo(AzureIcon.IconDictionary["logo"]),
                    Title = "Sign in",
                    Subtitle = "Sign into your Azure DevOps account",
                },
            };
        }
        else
        {
            var topLevelCommands = GetTopLevelSearches().GetAwaiter().GetResult();
            var defaultCommands = new List<ListItem>
            {
                new(_savedQueriesPage)
                {
                    Title = _resources.GetResource("Pages_Saved_Searches"),
                    Icon = new IconInfo("\ue721"),
                },
                new ListItem(_savedPullRequestSearchesPage)
                {
                    Title = "Save Pull Request Search",
                    Icon = new IconInfo("\ue721"),
                },
                new(_signOutPage)
                {
                    Title = _resources.GetResource("ExtensionTitle"),
                    Subtitle = _resources.GetResource("Forms_Sign_Out_Button_Title"),
                    Icon = new IconInfo(AzureIcon.IconDictionary["logo"]),
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
