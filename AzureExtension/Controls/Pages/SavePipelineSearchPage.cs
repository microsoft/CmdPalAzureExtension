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

    public SavePipelineSearchPage(IResources resources, SavePipelineSearchForm savePipelineSearchForm)
    {
        Title = "Save Pipeline Search";
        _resources = resources;
        _savePipelineSearchForm = savePipelineSearchForm;

        FormEventHelper.WireFormEvents(
            _savePipelineSearchForm,
            this,
            new StatusMessage(),
            "Pipeline search saved successfully!",
            "Error saving pipeline search: ");
    }

    public override IContent[] GetContent()
    {
        return new[] { _savePipelineSearchForm };
    }
}
