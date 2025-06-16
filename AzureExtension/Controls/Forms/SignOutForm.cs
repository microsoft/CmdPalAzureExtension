// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using AzureExtension.Account;
using AzureExtension.Controls.Commands;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Forms;

public sealed partial class SignOutForm : FormContent, IDisposable
{
    private readonly IResources _resources;
    private readonly SignOutCommand _signOutCommand;
    private readonly AuthenticationMediator _authenticationMediator;
    private readonly IAccountProvider _accountProvider;
    private bool _isButtonEnabled = true;

    public Dictionary<string, string> TemplateSubstitutions => new()
    {
        { "{{AuthTitle}}", _resources.GetResource("Forms_SignOut_TemplateAuthTitle") },
        { "{{AuthButtonTitle}}", AuthButtonTitle },
        { "{{AuthIcon}}", $"data:image/png;base64,{IconLoader.GetIconAsBase64("Logo")}" },
        { "{{AuthButtonTooltip}}", _resources.GetResource("Forms_SignOut_TemplateAuthButtonTooltip") },
        { "{{ButtonIsEnabled}}", IsButtonEnabled },
    };

    private string IsButtonEnabled =>
        _isButtonEnabled.ToString(CultureInfo.InvariantCulture).ToLower(CultureInfo.InvariantCulture);

    private string AuthButtonTitle =>
        string.IsNullOrEmpty(_accountProvider.GetDefaultAccount()?.Username) ? _resources.GetResource("Forms_SignOut_TemplateAuthButtonTitle_Success") : $"{_resources.GetResource("Forms_SignOut_TemplateAuthButtonTitle")} {_accountProvider.GetDefaultAccount()?.Username}";

    public SignOutForm(IResources resources, SignOutCommand signOutCommand, AuthenticationMediator authenticationMediator, IAccountProvider accountProvider)
    {
        _resources = resources;
        _signOutCommand = signOutCommand;
        _authenticationMediator = authenticationMediator;
        _authenticationMediator.LoadingStateChanged += OnLoadingStateChanged;
        _authenticationMediator.SignInAction += ResetButton;
        _authenticationMediator.SignOutAction += ResetButton;
        _accountProvider = accountProvider;
    }

    private void ResetButton(object? sender, SignInStatusChangedEventArgs e)
    {
        SetButtonEnabled(e.IsSignedIn);
    }

    private void OnLoadingStateChanged(object? sender, bool isLoading)
    {
        if (isLoading)
        {
            SetButtonEnabled(false);
        }
    }

    private void SetButtonEnabled(bool isEnabled)
    {
        _isButtonEnabled = isEnabled;
        TemplateJson = TemplateHelper.LoadTemplateJsonFromTemplateName("AuthTemplate", TemplateSubstitutions);
        OnPropertyChanged(nameof(TemplateJson));
    }

    public override string TemplateJson => TemplateHelper.LoadTemplateJsonFromTemplateName("AuthTemplate", TemplateSubstitutions);

    public override ICommandResult SubmitForm(string inputs, string data)
    {
       return _signOutCommand.Invoke();
    }

    // Disposing area
    private bool _disposed;

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _authenticationMediator.LoadingStateChanged -= OnLoadingStateChanged;
                _authenticationMediator.SignInAction -= ResetButton;
                _authenticationMediator.SignOutAction -= ResetButton;
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
