// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Account;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Commands;

public class SignOutCommand : InvokableCommand
{
    private readonly IResources _resources;
    private readonly IAccountProvider _accountProvider;
    private readonly AuthenticationMediator _authenticationMediator;
    private bool _invoked;

    public SignOutCommand(IResources resources, IAccountProvider accountProvider, AuthenticationMediator authenticationMediator)
    {
        _resources = resources;
        _accountProvider = accountProvider;
        _authenticationMediator = authenticationMediator;
        _authenticationMediator.SignInAction += ResetCommand;
        _authenticationMediator.SignOutAction += ResetCommand;
        Name = _resources.GetResource("Forms_SignOut_PageTitle");
        Icon = IconLoader.GetIcon("Logo");
    }

    private void ResetCommand(object? sender, SignInStatusChangedEventArgs e)
    {
        _invoked = !e.IsSignedIn;
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
                var accounts = await _accountProvider.GetLoggedInAccounts();

                foreach (var account in accounts)
                {
                    await _accountProvider.LogoutAccount(account.Username);
                }

                var signOutSucceeded = !_accountProvider.IsSignedIn();

                _authenticationMediator.SetLoadingState(false);
                _authenticationMediator.SignOut(new SignInStatusChangedEventArgs(!signOutSucceeded, null));
                ToastHelper.ShowToast(_resources.GetResource("Message_Sign_Out_Success"), MessageState.Success);
            }
            catch (Exception ex)
            {
                _authenticationMediator.SetLoadingState(false);

                // if sign out fails, the user is still signed in (true)
                _authenticationMediator.SignOut(new SignInStatusChangedEventArgs(true, ex));
                ToastHelper.ShowToast($"{_resources.GetResource("Message_Sign_Out_Fail")} {ex.Message}", MessageState.Error);
            }
        });

        _invoked = false;
        return CommandResult.KeepOpen();
    }
}
