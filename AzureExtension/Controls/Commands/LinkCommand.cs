// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Commands;

internal sealed partial class LinkCommand : InvokableCommand
{
    private readonly string _url;

    internal LinkCommand(string url, IResources resources, string? alternativeCommandName)
    {
        Name = string.IsNullOrEmpty(alternativeCommandName) ? resources.GetResource("Commands_Open_Link") : alternativeCommandName;
        Icon = IconLoader.GetIcon("OpenLink");
        _url = url;
    }

    public override CommandResult Invoke()
    {
        Process.Start(new ProcessStartInfo(_url) { UseShellExecute = true });
        return CommandResult.KeepOpen();
    }
}
