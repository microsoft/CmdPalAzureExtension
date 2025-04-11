// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Resources;
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

    public SaveQueryPage(SaveQueryForm saveQueryForm, StatusMessage statusMessage, string successMessage, string errorMessage, string saveQueryPageTitle)
    {
        _saveQueryForm = saveQueryForm;
        _statusMessage = statusMessage;
        _successMessage = successMessage;
        _errorMessage = errorMessage;
        Icon = new IconInfo("\uecc8");
        Title = saveQueryPageTitle;

        // Wire up events using the helper
        FormEventHelper.WireFormEvents(_saveQueryForm, this, _statusMessage, _successMessage, _errorMessage);

        // Hide status message initially
        ExtensionHost.HideStatus(_statusMessage);
    }

    public override IContent[] GetContent()
    {
        ExtensionHost.HideStatus(_statusMessage);
        return [_saveQueryForm];
    }
}
