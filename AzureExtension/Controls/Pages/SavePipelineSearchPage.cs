// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls.Forms;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Pages;

public class SavePipelineSearchPage : ContentPage
{
    private readonly IResources _resources;

    private readonly SavePipelineSearchForm _savePipelineSearchForm;

    private readonly StatusMessage _statusMessage;

    public SavePipelineSearchPage(IResources resources, SavePipelineSearchForm savePipelineSearchForm, StatusMessage statusMessage)
    {
        _resources = resources;
        _savePipelineSearchForm = savePipelineSearchForm;
        _statusMessage = statusMessage;
        Title = _resources.GetResource("Pages_SavePipelineSearch_Title");
        Icon = IconLoader.GetIcon("Add");

        FormEventHelper.WireFormEvents(
            _savePipelineSearchForm,
            this,
            _statusMessage,
            _resources.GetResource("Pages_SavePipelineSearch_SuccessMessage"),
            _resources.GetResource("Pages_SavePipelineSearch_FailureMessage"));

        ExtensionHost.HideStatus(_statusMessage);
    }

    public override IContent[] GetContent()
    {
        ExtensionHost.HideStatus(_statusMessage);
        return [_savePipelineSearchForm];
    }
}
