// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using AzureExtension.Account;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Commands;

public class SignInCommand : InvokableCommand, IDisposable
{
    private readonly IResources _resources;
    private readonly IAccountProvider _accountProvider;
    private readonly AuthenticationMediator _authenticationMediator;
    private bool _invoked;

    public SignInCommand(IResources resources, IAccountProvider accountProvider, AuthenticationMediator authenticationMediator)
    {
        _resources = resources;
        _accountProvider = accountProvider;
        _authenticationMediator = authenticationMediator;
        _authenticationMediator.SignInAction += ResetCommand;
        _authenticationMediator.SignOutAction += ResetCommand;
        Name = _resources.GetResource("Commands_SignIn");
        Icon = IconLoader.GetIcon("Logo");
    }

    private void ResetCommand(object? sender, SignInStatusChangedEventArgs e)
    {
        _invoked = e.IsSignedIn;
    }

    public override CommandResult Invoke()
    {
        if (_invoked)
        {
            return CommandResult.KeepOpen();
        }

        Task.Run(async () =>
        {
            _invoked = true;
            _authenticationMediator.SetLoadingState(true);
            try
            {
                await _accountProvider.ShowLogonSession();
                _authenticationMediator.SetLoadingState(false);
                _authenticationMediator.SignIn(new SignInStatusChangedEventArgs(true, null));
                ToastHelper.ShowToast(string.Format(CultureInfo.CurrentCulture, _resources.GetResource("Message_Sign_In_SuccessTemplate"), _accountProvider.GetDefaultAccount().Username), MessageState.Success);
            }
            catch (Exception ex)
            {
                _authenticationMediator.SetLoadingState(false);
                _authenticationMediator.SignIn(new SignInStatusChangedEventArgs(false, ex));
                ToastHelper.ShowToast($"{_resources.GetResource("Message_Sign_In_Fail")} {ex.Message}", MessageState.Error);
            }
        });
        return CommandResult.KeepOpen();
    }

    // Disposing area
    private bool _disposed;

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _authenticationMediator.SignInAction -= ResetCommand;
                _authenticationMediator.SignOutAction -= ResetCommand;
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
