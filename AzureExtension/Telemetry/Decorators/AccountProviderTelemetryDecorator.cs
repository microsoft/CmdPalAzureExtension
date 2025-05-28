// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Account;
using AzureExtension.Telemetry.Events;
using Microsoft.Identity.Client;
using Microsoft.VisualStudio.Services.Common;

namespace AzureExtension.Telemetry.Decorators;

public class AccountProviderTelemetryDecorator : IAccountProvider
{
    private readonly IAccountProvider _accountProvider;
    private readonly ITelemetryLogger _logger;

    public AccountProviderTelemetryDecorator(IAccountProvider accountProvider, ITelemetryLogger logger)
    {
        _accountProvider = accountProvider;
        _logger = logger;
    }

    public VssCredentials GetCredentials(IAccount account)
    {
        return _accountProvider.GetCredentials(account);
    }

    public Task<VssCredentials> GetCredentialsAsync(IAccount account)
    {
        return _accountProvider.GetCredentialsAsync(account);
    }

    public IAccount GetDefaultAccount()
    {
        return _accountProvider.GetDefaultAccount();
    }

    public Task<IAccount> GetDefaultAccountAsync()
    {
        return _accountProvider.GetDefaultAccountAsync();
    }

    public Task<IEnumerable<IAccount>> GetLoggedInAccounts()
    {
        return _accountProvider.GetLoggedInAccounts();
    }

    public bool IsSignedIn()
    {
        return _accountProvider.IsSignedIn();
    }

    public async Task<bool> LogoutAccount(string username)
    {
        var res = await _accountProvider.LogoutAccount(username);
        _logger.Log("LogoutAccount", LogLevel.Critical, new LogInOutTelemetryEvent());
        return res;
    }

    public async Task<IAccount> ShowLogonSession()
    {
        _logger.Log("ShowLogonSession", LogLevel.Critical, new LogInOutTelemetryEvent());
        var account = await _accountProvider.ShowLogonSession();
        _logger.Log("ShowLogonSessionCompleted", LogLevel.Critical, new LogInOutTelemetryEvent());
        return account;
    }
}
