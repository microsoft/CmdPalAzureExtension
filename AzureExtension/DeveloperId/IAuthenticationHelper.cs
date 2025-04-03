// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Identity.Client;
using Microsoft.UI;

namespace AzureExtension.DeveloperId;

public interface IAuthenticationHelper
{
    public AuthenticationSettings MicrosoftEntraIdSettings
    {
        get; set;
    }

    public void InitializePublicClientApplicationBuilder();

    public void InitializePublicClientAppForWAMBrokerAsync();

    public Task<IEnumerable<string>> GetAllStoredLoginIdsAsync();

    public Task InitializePublicClientAppForWAMBrokerAsyncWithParentWindow(IntPtr windowPtr);

    public Task<IAccount?> LoginDeveloperAccount(string[] scopes);

    public Task SignOutDeveloperIdAsync(string username);

    public Task<IAccount?> AcquireWindowsAccountTokenSilently(string[] scopes);

    public Task<IEnumerable<string>> AcquireAllDeveloperAccountTokens(string[] scopes);

    public Task<AuthenticationResult?> ObtainTokenForLoggedInDeveloperAccount(string[] scopes, string loginId);
}
