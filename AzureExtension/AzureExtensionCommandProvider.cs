// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Account;
using AzureExtension.Client;
using AzureExtension.Controls;
using AzureExtension.Controls.ListItems;
using AzureExtension.Controls.Pages;
using AzureExtension.DataManager;
using AzureExtension.DataManager.Cache;
using AzureExtension.Helpers;
using AzureExtension.PersistentData;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.Windows.ApplicationModel.Resources;

namespace AzureExtension;

public partial class AzureExtensionCommandProvider : CommandProvider
{
    private readonly SignInPage _signInPage;

    private readonly SignOutPage _signOutPage;

    private readonly SavedQueriesPage _savedQueriesPage;

    private readonly IAccountProvider _accountProvider;

    private readonly IResources _resources;

    private readonly AzureClientHelpers _azureClientHelpers;

    private readonly SavedPullRequestSearchesPage _savedPullRequestSearchesPage;

    private readonly ISearchPageFactory _searchPageFactory;

    private readonly SavedAzureSearchesMediator _mediator;

    public AzureExtensionCommandProvider(SignInPage signInPage, SignOutPage signOutPage, IAccountProvider accountProvider, SavedQueriesPage savedQueriesPage, IResources resources, AzureClientHelpers azureClientHelpers, SavedPullRequestSearchesPage savedPullRequestSearchesPage, ISearchPageFactory searchPageFactory, SavedAzureSearchesMediator mediator)
    {
        _signInPage = signInPage;
        _signOutPage = signOutPage;
        _accountProvider = accountProvider;
        _savedQueriesPage = savedQueriesPage;
        _resources = resources;
        _azureClientHelpers = azureClientHelpers;
        _savedPullRequestSearchesPage = savedPullRequestSearchesPage;
        _searchPageFactory = searchPageFactory;
        _mediator = mediator;
        DisplayName = "Azure Extension";

        var path = ResourceLoader.GetDefaultResourceFilePath();
        var resourceLoader = new ResourceLoader(path);

        _mediator.QuerySaved += OnSearchUpdated;
        _mediator.QueryRemoved += OnSearchUpdated;
        _mediator.PullRequestSearchSaved += OnSearchUpdated;
        _mediator.PullRequestSearchRemoved += OnSearchUpdated;
    }

    private void OnSearchUpdated(object? sender, object? args)
    {
        if (args is Controls.Query || args is PersistentData.Query
            || args is Controls.PullRequestSearch || args is PersistentData.PullRequestSearch)
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
