﻿// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.Identity.Client;
using Microsoft.VisualStudio.Services.Common;
using Windows.Foundation;

namespace AzureExtension.Account;

public interface IAccountProvider
{
    Task<IAccount> ShowLogonSession();

    Task<bool> LogoutAccount(string username);

    Task<IEnumerable<IAccount>> GetLoggedInAccounts();

    VssCredentials? GetCredentials(IAccount account);

    IAccount GetDefaultAccount();

    bool IsSignedIn();
}
