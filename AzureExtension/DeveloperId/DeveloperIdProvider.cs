// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Azure.Core;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using AzureExtension.Controls.Pages;
using AzureExtension.DataModel;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Serilog;
using Windows.Foundation;
using WinRT.Interop;

namespace AzureExtension.DeveloperId;

public class DeveloperIdProvider : IDeveloperIdProvider, IDisposable
{
    // Locks to control access to Singleton class members.
    private static readonly object _developerIdsLock = new();

    private static readonly object _authenticationProviderLock = new();

    // DeveloperId list containing all Logged in Ids.
    private List<DeveloperId> DeveloperIds
    {
        get; set;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    private readonly IAuthenticationHelper _developerIdAuthenticationHelper;

    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(DeveloperIdProvider));

    public event TypedEventHandler<IDeveloperIdProvider, IDeveloperId>? Changed;

    private readonly AuthenticationExperienceKind _authenticationExperienceForAzureExtension = AuthenticationExperienceKind.CustomProvider;

    public string DisplayName => "Azure";

    public event EventHandler<Exception?>? OAuthRedirected;

    public DeveloperIdProvider(IAuthenticationHelper authenticationHelper)
    {
        _log.Debug($"Creating DeveloperIdProvider singleton instance");

        lock (_developerIdsLock)
        {
            DeveloperIds ??= new List<DeveloperId>();

            _developerIdAuthenticationHelper = authenticationHelper;

            // Retrieve and populate Logged in DeveloperIds from previous launch.
            RestoreDeveloperIds(_developerIdAuthenticationHelper.GetAllStoredLoginIdsAsync());
        }
    }

    public void EnableSSOForAzureExtensionAsync()
    {
        var account = _developerIdAuthenticationHelper.AcquireWindowsAccountTokenSilently(_developerIdAuthenticationHelper.MicrosoftEntraIdSettings.ScopesArray);
        if (account.Result != null)
        {
            _ = CreateOrUpdateDeveloperId(account.Result);
            _log.Debug($"SSO For Azure Extension");
        }
    }

    // Retrieve access tokens for all accounts silently to determine application state
    // CommandPalette can use this information to inform and prompt user of next steps
    public DeveloperIdsResult DetermineAccountRemediationForAzureExtensionAsync()
    {
        var developerIds = new List<IDeveloperId>();
        var resultForAccountsToFix = _developerIdAuthenticationHelper.AcquireAllDeveloperAccountTokens(_developerIdAuthenticationHelper.MicrosoftEntraIdSettings.ScopesArray);
        var accountsToFix = resultForAccountsToFix.Result;
        if (accountsToFix.Any())
        {
            foreach (var account in accountsToFix)
            {
                var devId = GetDeveloperIdFromAccountIdentifier(account);
                if (devId != null)
                {
                    developerIds.Add(devId);
                }
                else
                {
                    _log.Warning($"DeveloperId not found to remediate");
                }
            }

            return new DeveloperIdsResult(developerIds);
        }

        return new DeveloperIdsResult(null, "No account remediation required");
    }

    public DeveloperIdsResult GetLoggedInDeveloperIds()
    {
        List<IDeveloperId> iDeveloperIds = new();
        lock (_developerIdsLock)
        {
            iDeveloperIds.AddRange(DeveloperIds);
        }

        var developerIdsResult = new DeveloperIdsResult(iDeveloperIds);

        return developerIdsResult;
    }

    public IAsyncOperation<DeveloperIdResult> ShowLogonSession()
    {
        return (IAsyncOperation<DeveloperIdResult>)Task.Run(async () =>
        {
            var hWnd = GetForegroundWindow();
            await _developerIdAuthenticationHelper.InitializePublicClientAppForWAMBrokerAsyncWithParentWindow(hWnd);
            var account = _developerIdAuthenticationHelper.LoginDeveloperAccount(_developerIdAuthenticationHelper.MicrosoftEntraIdSettings.ScopesArray);

            if (account.Result == null)
            {
                _log.Error($"Invalid AuthRequest");
                var exception = new InvalidOperationException();
                return new DeveloperIdResult(exception, "An issue has occurred with the authentication request");
            }

            _log.Information($"New DeveloperId logged in");

            var devId = CreateOrUpdateDeveloperId(account.Result);
            return new DeveloperIdResult(devId);
        });
    }

    /*
public IAsyncOperation<DeveloperIdResult> ShowLogonSession()
{
return Task.Run(async () =>
{
var windowPtr = GetConsoleOrTerminalWindow();
await _developerIdAuthenticationHelper.InitializePublicClientAppForWAMBrokerAsyncWithParentWindow(windowPtr);
var account = _developerIdAuthenticationHelper.LoginDeveloperAccount(_developerIdAuthenticationHelper.MicrosoftEntraIdSettings.ScopesArray);

if (account.Result == null)
{
    _log.Error($"Invalid AuthRequest");
    var exception = new InvalidOperationException();
    return new DeveloperIdResult(exception, "An issue has occurred with the authentication request");
}

_log.Information($"New DeveloperId logged in");

var pca = PublicClientApplicationBuilder.Create(_developerIdAuthenticationHelper.MicrosoftEntraIdSettings.ClientId)
    .WithAuthority("https://login.microsoftonline.com/72f988bf-86f1-41af-91ab-2d7cd011db47")
    .WithRedirectUri("http://localhost")
    .Build();

var account2 = await pca.AcquireTokenInteractive(_developerIdAuthenticationHelper.MicrosoftEntraIdSettings.ScopesArray)
    .WithUseEmbeddedWebView(false)
    .ExecuteAsync();

var devId = CreateOrUpdateDeveloperId(account2.Account);
return new DeveloperIdResult(devId);

// New stuff
string storageAccountName = "YOUR_STORAGE_ACCOUNT_NAME";
string containerName = "CONTAINER_NAME";

string appClientId = "ec33db7f-5b7e-4061-b729-8dab727cc764";
string resourceTenantId = "72f988bf-86f1-41af-91ab-2d7cd011db47";
Uri authorityUri = new($"https://login.microsoftonline.com/{resourceTenantId}");
string miClientId = "YOUR_MI_CLIENT_ID";
string audience = "api://AzureADTokenExchange";

// Get mi token to use as assertion
var miAssertionProvider = async (AssertionRequestOptions _) =>
{
    var miApplication = ManagedIdentityApplicationBuilder
        .Create(ManagedIdentityId.WithUserAssignedClientId(miClientId))
        .Build();

    var miResult = await miApplication.AcquireTokenForManagedIdentity(audience)
        .ExecuteAsync()
        .ConfigureAwait(false);
    return miResult.AccessToken;
};

// Create a confidential client application with the assertion.
IConfidentialClientApplication app = ConfidentialClientApplicationBuilder.Create(appClientId)
  .WithAuthority(authorityUri, false)
  .WithClientAssertion(miAssertionProvider)
  .WithCacheOptions(CacheOptions.EnableSharedCacheOptions)
  .Build();

// Get the federated app token for the storage account
string[] scopes = [$"https://{storageAccountName}.blob.core.windows.net/.default"];
AuthenticationResult result = await app.AcquireTokenForClient(scopes).ExecuteAsync().ConfigureAwait(false);

TokenCredential tokenCredential = new AccessTokenCredential(result.AccessToken);
var client = new BlobContainerClient(
    new Uri($"https://{storageAccountName}.blob.core.windows.net/{containerName}"),
    tokenCredential);

await foreach (BlobItem blob in containerClient.GetBlobsAsync())
{
    // TODO: perform operations with the blobs
    BlobClient blobClient = containerClient.GetBlobClient(blob.Name);
    Console.WriteLine($"Blob name: {blobClient.Name}, URI: {blobClient.Uri}");
}

}).AsAsyncOperation();
}*/

    public ProviderOperationResult LogoutDeveloperId(IDeveloperId developerId)
    {
        DeveloperId? developerIdToLogout;
        lock (_developerIdsLock)
        {
            developerIdToLogout = DeveloperIds?.Find(e => e.LoginId == developerId.LoginId);
            if (developerIdToLogout == null)
            {
                _log.Error($"Unable to find DeveloperId to logout");
                return new ProviderOperationResult(ProviderOperationStatus.Failure, new ArgumentNullException(nameof(developerId)), "The developer account to log out does not exist", "Unable to find DeveloperId to logout");
            }

            var result = _developerIdAuthenticationHelper.SignOutDeveloperIdAsync(developerIdToLogout.LoginId).GetAwaiter();
            DeveloperIds?.Remove(developerIdToLogout);
        }

        try
        {
            Changed?.Invoke(this, developerIdToLogout);
        }
        catch (Exception error)
        {
            _log.Error($"LoggedOut event signaling failed: {error}");
        }

        return new ProviderOperationResult(ProviderOperationStatus.Success, null, "The developer account has been logged out successfully", "LogoutDeveloperId succeeded");
    }

    public IEnumerable<IDeveloperId> GetLoggedInDeveloperIdsInternal()
    {
        List<DeveloperId> iDeveloperIds = new();
        lock (_developerIdsLock)
        {
            iDeveloperIds.AddRange(DeveloperIds);
        }

        return iDeveloperIds;
    }

    // Convert devID to internal devID.
    public IDeveloperId GetDeveloperIdInternal(IDeveloperId devId)
    {
        var devIds = GetLoggedInDeveloperIdsInternal();
        var devIdInternal = devIds.Where(i => i.LoginId.Equals(devId.LoginId, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

        return devIdInternal ?? throw new ArgumentException(devId.LoginId);
    }

    public IDeveloperId? GetDeveloperIdFromAccountIdentifier(string loginId)
    {
        var devIds = GetLoggedInDeveloperIdsInternal();
        var devIdInternal = devIds.Where(i => i.LoginId.Equals(loginId, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

        return devIdInternal;
    }

    // Internal Functions.
    private DeveloperId CreateOrUpdateDeveloperId(IAccount account)
    {
        // Query necessary data and populate Developer Id.
        DeveloperId newDeveloperId = new(account.Username, account.Username, account.Username, string.Empty, this);

        var duplicateDeveloperIds = DeveloperIds.Where(d => d.LoginId.Equals(newDeveloperId.LoginId, StringComparison.OrdinalIgnoreCase));

        if (duplicateDeveloperIds.Any())
        {
            _log.Information($"DeveloperID already exists! Updating accessToken");
            try
            {
                try
                {
                    Changed?.Invoke(this as IDeveloperIdProvider, duplicateDeveloperIds.Single() as IDeveloperId);
                }
                catch (Exception error)
                {
                    _log.Error($"Updated event signaling failed: {error}");
                }
            }
            catch (InvalidOperationException)
            {
                _log.Warning($"Multiple copies of same DeveloperID already exists");
                throw new InvalidOperationException("Multiple copies of same DeveloperID already exists");
            }
        }
        else
        {
            lock (_developerIdsLock)
            {
                DeveloperIds.Add(newDeveloperId);
            }

            try
            {
                Changed?.Invoke(this as IDeveloperIdProvider, newDeveloperId as IDeveloperId);
            }
            catch (Exception error)
            {
                _log.Error($"LoggedIn event signaling failed: {error}");
            }
        }

        return newDeveloperId;
    }

    private void RestoreDeveloperIds(Task<IEnumerable<string>> task)
    {
        var loginIds = task.Result;
        foreach (var loginId in loginIds)
        {
            DeveloperId developerId = new(loginId, loginId, loginId, string.Empty, this);

            lock (_developerIdsLock)
            {
                DeveloperIds.Add(developerId);
            }

            _log.Information($"Restored DeveloperId");
        }

        return;
    }

    internal void RefreshDeveloperId(IDeveloperId developerIdInternal)
    {
        Changed?.Invoke(this as IDeveloperIdProvider, developerIdInternal as IDeveloperId);
    }

    public AuthenticationExperienceKind GetAuthenticationExperienceKind()
    {
        return _authenticationExperienceForAzureExtension;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public AuthenticationState GetDeveloperIdState(IDeveloperId developerId)
    {
        DeveloperId? developerIdToFind;
        lock (_developerIdsLock)
        {
            developerIdToFind = DeveloperIds?.Find(e => e.LoginId == developerId.LoginId);
            if (developerIdToFind == null)
            {
                return AuthenticationState.LoggedOut;
            }
            else
            {
                return AuthenticationState.LoggedIn;
            }
        }
    }

    public AuthenticationResult? GetAuthenticationResultForDeveloperId(DeveloperId developerId)
    {
        try
        {
            var taskResult = _developerIdAuthenticationHelper.ObtainTokenForLoggedInDeveloperAccount(_developerIdAuthenticationHelper.MicrosoftEntraIdSettings.ScopesArray, developerId.LoginId);
            if (taskResult.Result != null)
            {
                return taskResult.Result;
            }
        }
        catch (MsalUiRequiredException ex)
        {
            _log.Error($"AcquireDeveloperAccountToken failed and requires user interaction {ex}");
            throw;
        }
        catch (MsalServiceException ex)
        {
            _log.Error($"AcquireDeveloperAccountToken failed with MSAL service error: {ex}");
            throw;
        }
        catch (MsalClientException ex)
        {
            _log.Error($"AcquireDeveloperAccountToken failed with MSAL client error: {ex}");
            throw;
        }
        catch (Exception ex)
        {
            _log.Error($"AcquireDeveloperAccountToken failed with error: {ex}");
            throw;
        }

        return null;
    }

    public AdaptiveCardSessionResult GetLoginAdaptiveCardSession() => throw new NotImplementedException();

    IDeveloperId IDeveloperIdProvider.GetDeveloperIdInternal(IDeveloperId devId)
    {
        return GetDeveloperIdInternal(devId);
    }

    public IAsyncOperation<IDeveloperId> LoginNewDeveloperIdAsync()
    {
        throw new NotImplementedException();
    }

    bool IDeveloperIdProvider.LogoutDeveloperId(IDeveloperId developerId)
    {
        throw new NotImplementedException();
    }

    public void HandleOauthRedirection(Uri authorizationResponse)
    {
        OAuthRedirected?.Invoke(this, null);
        throw new NotImplementedException();
    }

    public static IntPtr GetForegroundWindowHandle()
    {
        return GetForegroundWindow();
    }
}
