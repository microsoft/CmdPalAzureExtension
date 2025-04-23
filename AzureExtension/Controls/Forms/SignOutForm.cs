// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Account;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Forms;

public sealed partial class SignOutForm : FormContent, IAzureForm
{
    public static event EventHandler<SignInStatusChangedEventArgs>? SignOutAction;

    public event EventHandler<bool>? LoadingStateChanged;

    public event EventHandler<FormSubmitEventArgs>? FormSubmitted;

    private readonly IAccountProvider _accountProvider;
    private readonly IResources _resources;

    public SignOutForm(IAccountProvider accountProvider, IResources resources)
    {
        _accountProvider = accountProvider;
        _resources = resources;
    }

    public Dictionary<string, string> TemplateSubstitutions => new()
    {
        { "{{AuthTitle}}", _resources.GetResource("Forms_SignOut_TemplateAuthTitle") },
        { "{{AuthButtonTitle}}", _resources.GetResource("Forms_SignOut_TemplateAuthButtonTitle") },
        { "{{AuthIcon}}", $"data:image/png;base64,{AzureIcon.GetBase64Icon("logo")}" },
        { "{{AuthButtonTooltip}}", _resources.GetResource("Forms_SignOut_TemplateAuthButtonTooltip") },
        { "{{ButtonIsEnabled}}", "true" },
    };

    public override string TemplateJson => TemplateHelper.LoadTemplateJsonFromTemplateName("AuthTemplate", TemplateSubstitutions);

    public override ICommandResult SubmitForm(string inputs, string data)
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
                SignOutAction?.Invoke(this, new SignInStatusChangedEventArgs(!signOutSucceeded, null));
                FormSubmitted?.Invoke(this, new FormSubmitEventArgs(true, null));
            }
            catch (Exception ex)
            {
                LoadingStateChanged?.Invoke(this, false);

                // if sign out fails, the user is still signed in (true)
                SignOutAction?.Invoke(this, new SignInStatusChangedEventArgs(true, ex));
                FormSubmitted?.Invoke(this, new FormSubmitEventArgs(false, ex));
            }
        });
        return CommandResult.KeepOpen();
    }
}
