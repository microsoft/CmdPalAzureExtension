// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Account;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Commands;

public class SignInCommand : InvokableCommand
{
    private readonly IResources _resources;
    private readonly IAccountProvider _accountProvider;
    private readonly AuthenticationMediator _authenticationMediator;

    public event EventHandler<bool>? LoadingStateChanged;

    public SignInCommand(IResources resources, IAccountProvider accountProvider, AuthenticationMediator authenticationMediator)
    {
        _resources = resources;
        _accountProvider = accountProvider;
        _authenticationMediator = authenticationMediator;
        Name = _resources.GetResource("Forms_SignIn_PageTitle");
        Icon = IconLoader.GetIcon("Logo");
    }

    public override CommandResult Invoke()
    {
        LoadingStateChanged?.Invoke(this, true);
        Task.Run(async () =>
        {
            try
            {
                var signInSucceeded = await HandleSignIn();
                LoadingStateChanged?.Invoke(this, false);
                _authenticationMediator.SignIn(new SignInStatusChangedEventArgs(signInSucceeded, null));
                ToastHelper.ShowToast(_resources.GetResource("Message_Sign_In_Success"), MessageState.Success);
            }
            catch (Exception ex)
            {
                LoadingStateChanged?.Invoke(this, false);
                _authenticationMediator.SignIn(new SignInStatusChangedEventArgs(false, ex));
                ToastHelper.ShowToast($"{_resources.GetResource("Message_Sign_In_Fail")} {ex.Message}", MessageState.Error);
            }
        });
        return CommandResult.KeepOpen();
    }

    private async Task<bool> HandleSignIn()
    {
        try
        {
            var account = await _accountProvider.ShowLogonSession();
            return true;
        }
        catch (Exception ex)
        {
            var errorMessage = $"{ex.Message}";
            throw new InvalidOperationException(errorMessage);
        }
    }
}
