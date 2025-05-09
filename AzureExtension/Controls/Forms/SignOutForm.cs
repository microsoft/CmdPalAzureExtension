// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls.Commands;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Forms;

public sealed partial class SignOutForm : FormContent
{
    private readonly IResources _resources;
    private readonly SignOutCommand _signOutCommand;

    public SignOutForm(IResources resources, SignOutCommand signOutCommand)
    {
        _resources = resources;
        _signOutCommand = signOutCommand;
    }

    // ButtonIsEnabled is set to true by default. Nothing currently changes this value
    public Dictionary<string, string> TemplateSubstitutions => new()
    {
        { "{{AuthTitle}}", _resources.GetResource("Forms_SignOut_TemplateAuthTitle") },
        { "{{AuthButtonTitle}}", _resources.GetResource("Forms_SignOut_TemplateAuthButtonTitle") },
        { "{{AuthIcon}}", $"data:image/png;base64,{IconLoader.GetIconAsBase64("Logo")}" },
        { "{{AuthButtonTooltip}}", _resources.GetResource("Forms_SignOut_TemplateAuthButtonTooltip") },
        { "{{ButtonIsEnabled}}", "true" },
    };

    public override string TemplateJson => TemplateHelper.LoadTemplateJsonFromTemplateName("AuthTemplate", TemplateSubstitutions);

    public override ICommandResult SubmitForm(string inputs, string data)
    {
       return _signOutCommand.Invoke();
    }
}
