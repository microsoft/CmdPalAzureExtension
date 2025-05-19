// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls.Forms;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Pages;

public sealed partial class SaveQueryPage : ContentPage
{
    private readonly SaveQueryForm _saveQueryForm;
    private readonly StatusMessage _statusMessage;
    private readonly string _successMessage;
    private readonly string _errorMessage;
    private readonly IResources _resources;

    public SaveQueryPage(SaveQueryForm saveQueryForm, StatusMessage statusMessage, IResources resources)
    {
        _saveQueryForm = saveQueryForm;
        _statusMessage = statusMessage;
        _resources = resources;
        _successMessage = _resources.GetResource("Message_Query_Saved");
        _errorMessage = _resources.GetResource("Message_Query_Saved_Error");
        Icon = IconLoader.GetIcon("Add");
        Title = _resources.GetResource("Pages_SaveQuery_Title");

        FormEventHelper.WireFormEvents(_saveQueryForm, this, _statusMessage, _successMessage, _errorMessage);

        ExtensionHost.HideStatus(_statusMessage);
    }

    public override IContent[] GetContent()
    {
        ExtensionHost.HideStatus(_statusMessage);
        return [_saveQueryForm];
    }
}
