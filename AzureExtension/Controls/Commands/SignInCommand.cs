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
        Task.Run(async () =>
        {
            _authenticationMediator.SetLoadingState(true);
            try
            {
                await _accountProvider.ShowLogonSession();
                _authenticationMediator.SetLoadingState(false);
                _authenticationMediator.SignIn(new SignInStatusChangedEventArgs(true, null));
                ToastHelper.ShowToast(_resources.GetResource("Message_Sign_In_Success"), MessageState.Success);
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
}
