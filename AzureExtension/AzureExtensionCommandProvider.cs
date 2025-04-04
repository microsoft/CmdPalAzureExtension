// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls.Forms;
using AzureExtension.Controls.Pages;
using AzureExtension.DeveloperId;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.Windows.ApplicationModel.Resources;

namespace AzureExtension;

public partial class AzureExtensionCommandProvider : CommandProvider
{
    private readonly SignInPage _signInPage;
    private readonly TestPage _testPage;

    public AzureExtensionCommandProvider(SignInPage signInPage, TestPage testPage)
    {
        _signInPage = signInPage;
        _testPage = testPage;
        DisplayName = "Azure Extension";

        var path = ResourceLoader.GetDefaultResourceFilePath();
        var resourceLoader = new ResourceLoader(path);

        var authenticationHelper = new AuthenticationHelper();
    }

    public override ICommandItem[] TopLevelCommands() => [new CommandItem(_signInPage)
    {
        Title = "Azure extension: sign in",
        Icon = new IconInfo(AzureIcon.IconDictionary["logo"]),
    },
    new CommandItem(_testPage)
    {
        Title = "Azure extension: test",
        Icon = new IconInfo(AzureIcon.IconDictionary["logo"]),
    }
    ];
}
