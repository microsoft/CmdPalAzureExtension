// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls.Commands;
using AzureExtension.Controls.Forms;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Pages;

public sealed partial class SignOutPage : ContentPage, IDisposable
{
    private readonly SignOutForm _signOutForm;
    private readonly IResources _resources;
    private readonly SignOutCommand _signOutCommand;
    private readonly AuthenticationMediator _authenticationMediator; // corrected spelling

    public SignOutPage(SignOutForm signOutForm, IResources resources, SignOutCommand signOutCommand, AuthenticationMediator authenticationMediator)
    {
        _resources = resources;
        _signOutForm = signOutForm;
        _signOutForm.PropChanged += UpdatePage;
        _signOutCommand = signOutCommand;
        _authenticationMediator = authenticationMediator;
        _authenticationMediator.LoadingStateChanged += OnLoadingStateChanged;
        Icon = IconLoader.GetIcon("Logo");
        Title = _resources.GetResource("Pages_SignOut_Title");
        Name = Title; // Title is for the Page, Name is for the command

        Commands =
        [
            new CommandContextItem(_signOutCommand),
        ];
    }

    private void UpdatePage(object sender, IPropChangedEventArgs args)
    {
        RaiseItemsChanged();
    }

    private void OnLoadingStateChanged(object? sender, bool isLoading)
    {
        IsLoading = isLoading;
    }

    public override IContent[] GetContent()
    {
        return [_signOutForm];
    }

    // Disposing area
    private bool _disposed;

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _signOutForm.PropChanged -= UpdatePage;
                _authenticationMediator.LoadingStateChanged -= OnLoadingStateChanged;
            }

            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
