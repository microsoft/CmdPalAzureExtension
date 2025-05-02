// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls.Forms;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Pages;

public sealed partial class SignOutPage : ContentPage
{
    private readonly SignOutForm _signOutForm;
    private readonly StatusMessage _statusMessage;
    private readonly string _successMessage;
    private readonly string _errorMessage;
    private readonly IResources _resources;

    public SignOutPage(SignOutForm signOutForm, StatusMessage statusMessage, string successMessage, string errorMessage, IResources resources)
    {
        _resources = resources;
        _signOutForm = signOutForm;
        _statusMessage = statusMessage;
        _successMessage = successMessage;
        _errorMessage = errorMessage;
        Icon = IconLoader.GetIcon("Logo");
        Title = _resources.GetResource("ExtensionTitle");

        // Subtitle in CommandProvider = _resources.GetResource("ExtensionSubtitle"); - subtitle is not part of the page interface
        Name = _resources.GetResource("ExtensionTitle"); // Title is for the Page, Name is for the command

        // Wire up events using the helper
        FormEventHelper.WireFormEvents(_signOutForm, this, _statusMessage, _successMessage, _errorMessage);

        // Hide status message initially
        ExtensionHost.HideStatus(_statusMessage);
    }

    public override IContent[] GetContent()
    {
        ExtensionHost.HideStatus(_statusMessage);
        return [_signOutForm];
    }
}
