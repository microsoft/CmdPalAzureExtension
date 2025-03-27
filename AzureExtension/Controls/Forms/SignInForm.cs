// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using AzureExtension.DeveloperId;
using AzureExtension.Helpers;
using CommandPaletteAzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Forms;

public partial class SignInForm : FormContent, IAzureForm
{
    public static event EventHandler<SignInStatusChangedEventArgs>? SignInAction;

    public event EventHandler<bool>? LoadingStateChanged;

    public event EventHandler<FormSubmitEventArgs>? FormSubmitted;

    private readonly IDeveloperIdProvider _developerIdProvider;

    private bool _isButtonEnabled = true;

    private string IsButtonEnabled =>
        _isButtonEnabled.ToString(CultureInfo.InvariantCulture).ToLower(CultureInfo.InvariantCulture);

    public SignInForm(IDeveloperIdProvider developerIdProvider)
    {
        _developerIdProvider = developerIdProvider;
        _developerIdProvider.OAuthRedirected += DeveloperIdProvider_OAuthRedirected;
        SignOutForm.SignOutAction += SignOutForm_SignOutAction;
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
        { "{{AuthTitle}}", Resources.GetResource("Forms_Sign_In") },
        { "{{AuthButtonTitle}}", Resources.GetResource("Forms_Sign_In") },
        { "{{AuthIcon}}", $"data:image/png;base64,{AzureIcon.GetBase64Icon("logo")}" },
        { "{{AuthButtonTooltip}}", Resources.GetResource("Forms_Sign_In_Tooltip") },
        { "{{ButtonIsEnabled}}", IsButtonEnabled },
    };

    public override string TemplateJson => TemplateHelper.LoadTemplateJsonFromTemplateName("AuthTemplate", TemplateSubstitutions);

    public override ICommandResult SubmitForm(string inputs, string data)
    {
        LoadingStateChanged?.Invoke(this, true);
        Task.Run(() =>
        {
            try
            {
                var signInSucceeded = HandleSignIn().Result;
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
        var numPreviousDevIds = _developerIdProvider.GetLoggedInDeveloperIdsInternal().Count();

        await _developerIdProvider.LoginNewDeveloperIdAsync();

        var numDevIds = _developerIdProvider.GetLoggedInDeveloperIdsInternal().Count();

        await _developerIdProvider.ShowLogonSession();

        return numDevIds > numPreviousDevIds;
    }
}
