// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls.Pages;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension;

public partial class AzureExtensionActionsProvider : CommandProvider
{
    private readonly AzureExtensionPage _azureExtensionPage;

    public AzureExtensionActionsProvider(AzureExtensionPage azureExtensionPage)
    {
        _azureExtensionPage = azureExtensionPage;
        DisplayName = "Azure extension for cmdpal Commands";
    }

    public override ICommandItem[] TopLevelCommands()
    {
        return [
            new CommandItem(_azureExtensionPage),
        ];
    }
}
