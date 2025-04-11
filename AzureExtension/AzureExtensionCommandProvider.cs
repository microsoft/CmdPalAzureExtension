// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Account;
using AzureExtension.Client;
using AzureExtension.Controls.Pages;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.Windows.ApplicationModel.Resources;

namespace AzureExtension;

public partial class AzureExtensionCommandProvider : CommandProvider
{
    private readonly SignInPage _signInPage;

    private readonly SignOutPage _signOutPage;

    private readonly SavedQueriesPage _savedSearchesPage;

    private readonly IAccountProvider _accountProvider;

    private readonly IResources _resources;

    private readonly AzureClientHelpers _azureClientHelpers;

    public AzureExtensionCommandProvider(SignInPage signInPage, SignOutPage signOutPage, IAccountProvider accountProvider, SavedQueriesPage savedSearchesPage, IResources resources, AzureClientHelpers azureClientHelpers)
    {
        _signInPage = signInPage;
        _signOutPage = signOutPage;
        _accountProvider = accountProvider;
        _savedSearchesPage = savedSearchesPage;
        _resources = resources;
        _azureClientHelpers = azureClientHelpers;
        DisplayName = "Azure Extension";

        var path = ResourceLoader.GetDefaultResourceFilePath();
        var resourceLoader = new ResourceLoader(path);
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
            var account = _accountProvider.GetDefaultAccount();
            var selectedQueryUrl = new AzureUri("https://microsoft.visualstudio.com/OS/_queries/query-edit/fd7ad0f5-17b0-46be-886a-92e4043c1c4f/");
            var queryInfo = _azureClientHelpers.GetQueryInfo(selectedQueryUrl, account);

            var defaultCommands = new List<CommandItem>
            {
                new(_savedSearchesPage)
                {
                    Title = _resources.GetResource("Pages_Saved_Searches"),
                    Icon = new IconInfo("\ue721"),
                },
                new(_signOutPage)
                {
                    Title = _resources.GetResource("ExtensionTitle"),
                    Subtitle = _resources.GetResource("Forms_Sign_Out_Button_Title"),
                    Icon = new IconInfo(AzureIcon.IconDictionary["logo"]),
                },
            };

            return defaultCommands.ToArray();
        }
    }
}
