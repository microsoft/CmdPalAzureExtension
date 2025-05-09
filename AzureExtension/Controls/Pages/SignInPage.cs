// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls.Commands;
using AzureExtension.Controls.Forms;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Pages;

public partial class SignInPage : ContentPage
{
    private readonly SignInForm _signInForm;
    private readonly StatusMessage _statusMessage;
    private readonly string _successMessage;
    private readonly string _errorMessage;
    private readonly IResources _resources;
    private readonly SignInCommand _signInCommand;
    private readonly AuthenticationMediator _authenticationMediator;

    public SignInPage(SignInForm signInForm, StatusMessage statusMessage, IResources resources, SignInCommand signInCommand, AuthenticationMediator authenticationMediator)
    {
        _resources = resources;
        Icon = IconLoader.GetIcon("Logo");
        Title = _resources.GetResource("Forms_SignIn_PageTitle");
        Name = Title; // Title is for the Page, Name is for the command
        _signInForm = signInForm;
        _statusMessage = statusMessage;
        _successMessage = resources.GetResource("Message_Sign_In_Success");
        _errorMessage = resources.GetResource("Message_Sign_In_Fail");
        _signInCommand = signInCommand;
        _authenticationMediator = authenticationMediator;
        _authenticationMediator.LoadingStateChanged += OnLoadingStateChanged;

        // Wire up events using the helper
        // FormEventHelper.WireFormEvents(_signInForm, this, _statusMessage, _successMessage, _errorMessage);
        _signInForm.PropChanged += UpdatePage;

        // Hide status message initially
        ExtensionHost.HideStatus(_statusMessage);

        Commands = new ICommandContextItem[]
        {
            new CommandContextItem(_signInCommand),
        };
    }

    private void OnLoadingStateChanged(object? sender, bool isLoading)
    {
        IsLoading = isLoading;
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
}
