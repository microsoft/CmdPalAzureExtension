// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Commands;

internal sealed partial class CopyCommand : InvokableCommand
{
    private readonly string _valueToCopy;

    internal CopyCommand(string valueToCopy, string valueToCopyString)
    {
        _valueToCopy = valueToCopy;
        Name = valueToCopyString;
        Icon = IconLoader.GetIcon("Copy");
    }

    public override CommandResult Invoke()
    {
        ClipboardHelper.SetText(_valueToCopy);
        return CommandResult.Dismiss();
    }
}
