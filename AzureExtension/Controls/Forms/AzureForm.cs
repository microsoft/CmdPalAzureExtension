// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Forms;

public abstract class AzureForm : FormContent
{
    public Dictionary<string, string> TemplateSubstitutions { get; set; }

    public string TemplateKey { get; set; } = string.Empty;

    public AzureForm()
    {
        TemplateSubstitutions = new Dictionary<string, string>();
    }

    public override string TemplateJson => TemplateHelper.LoadTemplateJsonFromTemplateName(TemplateKey, TemplateSubstitutions);

    public override ICommandResult SubmitForm(string inputs, string data)
    {
        LoadingStateChanged?.Invoke(this, true);
        Task.Run(async () =>
        {
            await HandleInputs(inputs);
        });

        return CommandResult.KeepOpen();
    }

    public abstract Task HandleInputs(string inputs);
}
