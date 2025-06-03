// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls.Forms;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls;

internal sealed partial class EditQueryPage : ContentPage
{
    private readonly IResources _resources;
    private readonly SaveQueryForm _saveQueryForm;
    private readonly StatusMessage _statusMessage;
    private readonly string _successMessage;
    private readonly string _errorMessage;

    public EditQueryPage(IResources resources, SaveQueryForm saveQueryForm, StatusMessage statusMessage)
    {
        _resources = resources;
        _saveQueryForm = saveQueryForm;
        _statusMessage = statusMessage;
        _successMessage = _resources.GetResource("Pages_EditQuery_SuccessMessage");
        _errorMessage = _resources.GetResource("Pages_EditQuery_FailureMessage");

        FormEventHelper.WireFormEvents(_saveQueryForm, this, _statusMessage, _successMessage, _errorMessage);

        ExtensionHost.HideStatus(_statusMessage);

        Title = _resources.GetResource("Pages_EditQuery");
        Name = Title; // Title is for the Page, Name is for the Command
        Icon = IconLoader.GetIcon("Edit");
    }

    public override IContent[] GetContent()
    {
        ExtensionHost.HideStatus(_statusMessage);
        return [_saveQueryForm];
    }
}
