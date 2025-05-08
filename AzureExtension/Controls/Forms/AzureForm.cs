// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Forms;

public abstract class AzureForm : FormContent
{
    public string TemplateKey { get; set; } = string.Empty;

    public AzureForm()
    {
    }

    public override ICommandResult SubmitForm(string inputs, string data)
    {
        Task.Run(async () =>
        {
            await HandleInputs(inputs);
        });

        return CommandResult.KeepOpen();
    }

    public abstract Task HandleInputs(string inputs);
}
