﻿// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Commands;

internal sealed partial class CopyCommand : InvokableCommand
{
    private readonly string _valueToCopy;
    private readonly IResources _resources;

    internal CopyCommand(string valueToCopy, string copyCommandName, IResources resources)
    {
        _valueToCopy = valueToCopy;
        Name = copyCommandName;
        Icon = IconLoader.GetIcon("Copy");
        _resources = resources;
    }

    public override CommandResult Invoke()
    {
        ClipboardHelper.SetText(_valueToCopy);
        ToastHelper.ShowSuccessToast(_resources.GetResource("Messages_CopyCommand_Success"));

        Thread.Sleep(1500); // Pause to allow the toast to show before dismissing the command

        return CommandResult.Dismiss();
    }
}
