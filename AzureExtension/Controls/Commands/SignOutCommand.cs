// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Account;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Commands;

public class SignOutCommand : InvokableCommand
{
    private readonly IResources _resources;
    private readonly IAccountProvider _accountProvider;
    private readonly AuthenticationMediator _authenticationMediator;

    public event EventHandler<bool>? LoadingStateChanged;

    public event EventHandler<FormSubmitEventArgs>? FormSubmitted;

    public SignOutCommand(IResources resources, IAccountProvider accountProvider, AuthenticationMediator authenticationMediator)
    {
        _resources = resources;
        _accountProvider = accountProvider;
        _authenticationMediator = authenticationMediator;
        Name = _resources.GetResource("Forms_SignOut_PageTitle");
        Icon = IconLoader.GetIcon("Logo");
    }

    public override CommandResult Invoke()
    {
        LoadingStateChanged?.Invoke(this, true);
        Task.Run(async () =>
        {
            try
            {
                var accounts = await _accountProvider.GetLoggedInAccounts();

                foreach (var account in accounts)
                {
                    await _accountProvider.LogoutAccount(account.Username);
                }

                var signOutSucceeded = !_accountProvider.IsSignedIn();

                LoadingStateChanged?.Invoke(this, false);
                _authenticationMediator.SignOut(new SignInStatusChangedEventArgs(!signOutSucceeded, null));
                FormSubmitted?.Invoke(this, new FormSubmitEventArgs(true, null));
            }
            catch (Exception ex)
            {
                LoadingStateChanged?.Invoke(this, false);

                // if sign out fails, the user is still signed in (true)
                _authenticationMediator.SignOut(new SignInStatusChangedEventArgs(true, ex));
                FormSubmitted?.Invoke(this, new FormSubmitEventArgs(false, ex));
            }
        });
        return CommandResult.KeepOpen();
    }
}
