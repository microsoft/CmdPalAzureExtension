// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls.Forms;
using AzureExtension.DeveloperId;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Windows.Foundation;

namespace AzureExtension.Controls.Pages;

public partial class SignInPage : ContentPage
{
    private readonly IDeveloperIdProvider _developerIdProvider;
    private readonly SignInForm _signInForm;
    private readonly StatusMessage _statusMessage;
    private readonly string _successMessage;
    private readonly string _errorMessage;

    public SignInPage(SignInForm signInForm, StatusMessage statusMessage, string successMessage, string errorMessage, IDeveloperIdProvider developerIdProvider)
    {
        _developerIdProvider = developerIdProvider;
        _signInForm = signInForm;
        _statusMessage = statusMessage;
        _successMessage = successMessage;
        _errorMessage = errorMessage;

        // Wire up events using the helper
        FormEventHelper.WireFormEvents(_signInForm, this, _statusMessage, _successMessage, _errorMessage);

        _signInForm.PropChanged += UpdatePage;

        // Hide status message initially
        ExtensionHost.HideStatus(_statusMessage);
    }

    private void UpdatePage(object sender, IPropChangedEventArgs args)
    {
        RaiseItemsChanged();
    }

    public override IContent[] GetContent()
    {
        ExtensionHost.HideStatus(_statusMessage);
        return [_signInForm];
    }

    protected virtual bool IsUserLoggedIn()
    {
        // User is not logged in if either there are zero DeveloperIds logged in, or the selected
        // DeveloperId for this widget is not logged in.
        var authProvider = _developerIdProvider;
        if (!authProvider.GetLoggedInDeveloperIds().DeveloperIds.Any())
        {
            return false;
        }

        return true;

        /*
        if (!DeveloperIdLoginRequired)
        {
            // At least one user is logged in, and this widget does not require a specific
            // DeveloperId so we are in a good state.
            return true;
        }

        if (string.IsNullOrEmpty(DeveloperLoginId))
        {
            // User has not yet chosen a DeveloperId, but there is at least one available, so the
            // user has logged in and we are in a good state.
            return true;
        }

        if (GetDevId(DeveloperLoginId) is not null)
        {
            // The selected DeveloperId is logged in so we are in a good state.
            return true;
        }

        return false;
        */
    }

    protected IDeveloperId? GetDevId(string login)
    {
        var devIdProvider = _developerIdProvider;
        IDeveloperId? developerId = null;

        foreach (var devId in devIdProvider.GetLoggedInDeveloperIds().DeveloperIds)
        {
            if (devId.LoginId == login)
            {
                developerId = devId;
            }
        }

        return developerId;
    }
}
