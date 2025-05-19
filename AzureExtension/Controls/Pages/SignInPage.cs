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
    private readonly IResources _resources;
    private readonly SignInCommand _signInCommand;
    private readonly AuthenticationMediator _authenticationMediator;

    public SignInPage(SignInForm signInForm, IResources resources, SignInCommand signInCommand, AuthenticationMediator authenticationMediator)
    {
        _resources = resources;
        Icon = IconLoader.GetIcon("Logo");
        Title = _resources.GetResource("Pages_SignIn_Title");
        Name = Title; // Title is for the Page, Name is for the command
        _signInForm = signInForm;
        _signInCommand = signInCommand;
        _authenticationMediator = authenticationMediator;
        _authenticationMediator.LoadingStateChanged += OnLoadingStateChanged;

        _signInForm.PropChanged += UpdatePage;

        Commands =
        [
            new CommandContextItem(_signInCommand),
        ];
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
        return [_signInForm];
    }
}
