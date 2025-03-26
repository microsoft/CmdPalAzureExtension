// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

<<<<<<< HEAD
=======
using Microsoft.Identity.Client;
>>>>>>> main
using Windows.Foundation;

namespace AzureExtension.DeveloperId;

public interface IDeveloperIdProvider
{
<<<<<<< HEAD
=======
    event TypedEventHandler<IDeveloperIdProvider, IDeveloperId>? Changed;

>>>>>>> main
    IEnumerable<IDeveloperId> GetLoggedInDeveloperIdsInternal();

    IDeveloperId GetDeveloperIdInternal(IDeveloperId devId);

    IAsyncOperation<IDeveloperId> LoginNewDeveloperIdAsync();

    bool LogoutDeveloperId(IDeveloperId developerId);

    void HandleOauthRedirection(Uri authorizationResponse);

<<<<<<< HEAD
    public event EventHandler<Exception?>? OAuthRedirected;
=======
    AuthenticationState GetDeveloperIdState(IDeveloperId developerId);

    IDeveloperId? GetDeveloperIdFromAccountIdentifier(string loginId);

    AuthenticationResult? GetAuthenticationResultForDeveloperId(DeveloperId developerId);
>>>>>>> main
}
