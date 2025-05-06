// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls.Forms;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls;

internal sealed partial class EditPipelineSearchPage : ContentPage
{
    private readonly IResources _resources;
    private readonly SavePipelineSearchForm _savePipelineSearchForm;
    private readonly StatusMessage _statusMessage;
    private readonly string _successMessage;
    private readonly string _errorMessage;

    public EditPipelineSearchPage(IResources resources, SavePipelineSearchForm savePipelineSearchForm, StatusMessage statusMessage, string successMessage, string errorMessage)
    {
        _resources = resources;
        _savePipelineSearchForm = savePipelineSearchForm;
        _statusMessage = statusMessage;
        _successMessage = successMessage;
        _errorMessage = errorMessage;

        // Wire up events using the helper
        FormEventHelper.WireFormEvents(_savePipelineSearchForm, this, _statusMessage, _successMessage, _errorMessage);

        // Hide status message initially
        ExtensionHost.HideStatus(_statusMessage);

        // Set page properties
        Title = _resources.GetResource("Pages_Edit");
        Name = _resources.GetResource("Pages_Edit"); // Title is for the Page, Name is for the Command
        Icon = IconLoader.GetIcon("Edit");
    }

    public override IContent[] GetContent()
    {
        ExtensionHost.HideStatus(_statusMessage);
        return [_savePipelineSearchForm];
    }
}
