// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension;

public partial class AzureExtensionActionsProvider : CommandProvider
{
    public AzureExtensionActionsProvider()
    {
        DisplayName = "Azure extension for cmdpal Commands";
    }

    private readonly ICommandItem[] _commands = [
        new CommandItem(new AzureExtensionPage()),
    ];

    public override ICommandItem[] TopLevelCommands()
    {
        return _commands;
    }
}
