// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using AzureExtension.Controls.Commands;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Forms;

public partial class SignInForm : FormContent, IDisposable
{
    private readonly IResources _resources;
    private readonly AuthenticationMediator _authenticationMediator;
    private readonly SignInCommand _signInCommand;

    private bool _isButtonEnabled = true;

    private string IsButtonEnabled =>
        _isButtonEnabled.ToString(CultureInfo.InvariantCulture).ToLower(CultureInfo.InvariantCulture);

    public Dictionary<string, string> TemplateSubstitutions => new()
    {
        { "{{AuthTitle}}", _resources.GetResource("Forms_SignIn_TemplateAuthTitle") },
        { "{{AuthButtonTitle}}", _resources.GetResource("Forms_SignIn_TemplateAuthButtonTitle") },
        { "{{AuthIcon}}", $"data:image/png;base64,{IconLoader.GetIconAsBase64("Logo")}" },
        { "{{AuthButtonTooltip}}", _resources.GetResource("Forms_SignIn_TemplateAuthButtonTooltip") },
        { "{{ButtonIsEnabled}}", IsButtonEnabled },
    };

    public SignInForm(AuthenticationMediator authenticationMediator, IResources resources, SignInCommand signInCommand)
    {
        _authenticationMediator = authenticationMediator;
        _authenticationMediator.LoadingStateChanged += OnLoadingStateChanged;
        _authenticationMediator.SignInAction += ResetButton;
        _authenticationMediator.SignOutAction += ResetButton;
        _resources = resources;
        _signInCommand = signInCommand;
    }

    private void ResetButton(object? sender, SignInStatusChangedEventArgs e)
    {
        SetButtonEnabled(!e.IsSignedIn);
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
        return _signInCommand.Invoke();
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
