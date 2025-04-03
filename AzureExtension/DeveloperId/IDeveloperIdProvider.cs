// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.Identity.Client;
using Windows.Foundation;

namespace AzureExtension.DeveloperId;

public interface IDeveloperIdProvider
{
    event TypedEventHandler<IDeveloperIdProvider, IDeveloperId>? Changed;

    IEnumerable<IDeveloperId> GetLoggedInDeveloperIdsInternal();

    IDeveloperId GetDeveloperIdInternal(IDeveloperId devId);

    IAsyncOperation<IDeveloperId> LoginNewDeveloperIdAsync();

    bool LogoutDeveloperId(IDeveloperId developerId);

    void HandleOauthRedirection(Uri authorizationResponse);

    event EventHandler<Exception?>? OAuthRedirected;

    DeveloperIdsResult GetLoggedInDeveloperIds();

    AuthenticationState GetDeveloperIdState(IDeveloperId developerId);

    IDeveloperId? GetDeveloperIdFromAccountIdentifier(string loginId);

    AuthenticationResult? GetAuthenticationResultForDeveloperId(DeveloperId developerId);

    public IAsyncOperation<DeveloperIdResult> ShowLogonSession();
}
