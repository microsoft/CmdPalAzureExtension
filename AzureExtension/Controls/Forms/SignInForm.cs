// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using AzureExtension.Account;
using AzureExtension.Client;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Forms;

public partial class SignInForm : FormContent, IAzureForm
{
    public static event EventHandler<SignInStatusChangedEventArgs>? SignInAction;

    public event EventHandler<bool>? LoadingStateChanged;

    public event EventHandler<FormSubmitEventArgs>? FormSubmitted;

    private readonly IAccountProvider _accountProvider;
    private readonly AzureClientHelpers _azureClientHelpers;

    private bool _isButtonEnabled = true;

    private string IsButtonEnabled =>
        _isButtonEnabled.ToString(CultureInfo.InvariantCulture).ToLower(CultureInfo.InvariantCulture);

    private Page? page;

    public SignInForm(IAccountProvider accountProvider, AzureClientHelpers azureClientHelpers)
    {
        _accountProvider = accountProvider;
        SignOutForm.SignOutAction += SignOutForm_SignOutAction;
        page = null;
        _azureClientHelpers = azureClientHelpers;
    }

    public void SetPage(Page page)
    {
        this.page = page;
    }

    private void SignOutForm_SignOutAction(object? sender, SignInStatusChangedEventArgs e)
    {
        _isButtonEnabled = !e.IsSignedIn;
    }

    private void DeveloperIdProvider_OAuthRedirected(object? sender, Exception? e)
    {
        if (e is not null)
        {
            SetButtonEnabled(true);
            LoadingStateChanged?.Invoke(this, false);
            SignInAction?.Invoke(this, new SignInStatusChangedEventArgs(false, e));
            FormSubmitted?.Invoke(this, new FormSubmitEventArgs(false, e));
            return;
        }

        SetButtonEnabled(false);
    }

    private void SetButtonEnabled(bool isEnabled)
    {
        _isButtonEnabled = isEnabled;
        TemplateJson = TemplateHelper.LoadTemplateJsonFromTemplateName("AuthTemplate", TemplateSubstitutions);
        OnPropertyChanged(nameof(TemplateJson));
    }

    public Dictionary<string, string> TemplateSubstitutions => new()
    {
        { "{{AuthTitle}}", "Sign into your ADO account" },
        { "{{AuthButtonTitle}}", "Sign in" },
        { "{{AuthIcon}}", $"data:image/png;base64,{AzureIcon.GetBase64Icon("logo")}" },
        { "{{AuthButtonTooltip}}", "tooltip" },
        { "{{ButtonIsEnabled}}", IsButtonEnabled },
    };

    public override string TemplateJson => TemplateHelper.LoadTemplateJsonFromTemplateName("AuthTemplate", TemplateSubstitutions);

    public override ICommandResult SubmitForm(string inputs, string data)
    {
        LoadingStateChanged?.Invoke(this, true);
        Task.Run(async () =>
        {
            try
            {
                var signInSucceeded = await HandleSignIn();
                LoadingStateChanged?.Invoke(this, false);
                SignInAction?.Invoke(this, new SignInStatusChangedEventArgs(signInSucceeded, null));
                FormSubmitted?.Invoke(this, new FormSubmitEventArgs(signInSucceeded, null));
            }
            catch (Exception ex)
            {
                LoadingStateChanged?.Invoke(this, false);
                SetButtonEnabled(true);
                SignInAction?.Invoke(this, new SignInStatusChangedEventArgs(false, ex));
                FormSubmitted?.Invoke(this, new FormSubmitEventArgs(false, ex));
            }
        });
        return CommandResult.KeepOpen();
    }

    private async Task<bool> HandleSignIn()
    {
        try
        {
            var account = await _accountProvider.ShowLogonSession();
            var selectedQueryUrl = new AzureUri("https://microsoft.visualstudio.com/OS/_queries/query-edit/fd7ad0f5-17b0-46be-886a-92e4043c1c4f/");
            var queryInfo = _azureClientHelpers.GetQueryInfo(selectedQueryUrl, account);
            var selectedQueryId = queryInfo.AzureUri.Query;
            return true;
        }
        catch (Exception ex)
        {
            var errorMessage = $"{ex.Message}";
            throw new InvalidOperationException(errorMessage);
        }
    }
}
