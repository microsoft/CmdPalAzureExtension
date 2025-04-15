// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls.Forms;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Pages;

public class SavePullRequestSearchPage : ContentPage
{
    private readonly StatusMessage _statusMessage;

    private readonly SavePullRequestSearchForm _savePullRequestSearchForm;

    public SavePullRequestSearchPage(SavePullRequestSearchForm savePullRequestSearchForm, StatusMessage statusMessage)
    {
        Title = "Save Pull Request";
        Icon = new IconInfo("\uecc8");
        _savePullRequestSearchForm = savePullRequestSearchForm;
        _statusMessage = statusMessage;

        // Wire up events using the helper
        FormEventHelper.WireFormEvents(_savePullRequestSearchForm, this, _statusMessage, "success!", "failure");

        // Hide status message initially
        ExtensionHost.HideStatus(_statusMessage);
    }

    public override IContent[] GetContent()
    {
        ExtensionHost.HideStatus(_statusMessage);
        return [_savePullRequestSearchForm];
    }
}
