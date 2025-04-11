// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Forms;

public class SavePullRequestSearchForm : FormContent, IAzureForm
{
    private readonly IResources _resources;

    public event EventHandler<bool>? LoadingStateChanged;

    public event EventHandler<FormSubmitEventArgs>? FormSubmitted;

    public Dictionary<string, string> TemplateSubstitutions => new()
    {
        { "${url}", string.Empty },
        { "${widgetTitle}", string.Empty },
    };

    public override string TemplateJson => TemplateHelper.LoadTemplateJsonFromTemplateName("SavePullRequestSearch", TemplateSubstitutions);

    public SavePullRequestSearchForm(IResources resources)
    {
        LoadingStateChanged?.Invoke(this, false);
        FormSubmitted?.Invoke(this, new FormSubmitEventArgs(true, null));
        _resources = resources;
    }
}
