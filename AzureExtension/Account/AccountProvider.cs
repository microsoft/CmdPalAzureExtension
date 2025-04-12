// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Broker;
using Microsoft.Identity.Client.Extensions.Msal;
using Microsoft.IdentityModel.Abstractions;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.Common;
using Serilog;

namespace AzureExtension.Account;

public class AccountProvider : IAccountProvider
{
    private readonly AuthenticationSettings _microsoftEntraIdSettings;

    private static readonly string[] _capabilities = ["cp1"];

    public static Guid MSATenetId { get; } = new("9188040d-6c67-4c5b-b112-36a304b66dad");

    public static Guid TransferTenetId { get; } = new("f8cdef31-a31e-4b4a-93e4-5f571e91255a");

    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(IAccountProvider));

    private PublicClientApplicationBuilder? PublicClientApplicationBuilder
    {
        get; set;
    }

    public IPublicClientApplication? PublicClientApplication
    {
        get; private set;
    }

    public string DisplayName => "Azure";

    public AccountProvider(AuthenticationSettings microsoftEntraIdSettings)
    {
        _microsoftEntraIdSettings = microsoftEntraIdSettings;

        InitializePublicClientApplicationBuilder();

        InitializePublicClientAppForWAMBrokerAsync();
    }

    public void InitializePublicClientApplicationBuilder()
    {
        PublicClientApplicationBuilder = PublicClientApplicationBuilder.Create(_microsoftEntraIdSettings.ClientId)
           .WithAuthority(string.Format(CultureInfo.InvariantCulture, _microsoftEntraIdSettings.Authority, _microsoftEntraIdSettings.TenantId))
           .WithRedirectUri(string.Format(CultureInfo.InvariantCulture, _microsoftEntraIdSettings.RedirectURI, _microsoftEntraIdSettings.ClientId))
           .WithLogging(new MSALLogger(EventLogLevel.Warning), enablePiiLogging: false)
           .WithClientCapabilities(_capabilities);
        _log.Debug($"Created PublicClientApplicationBuilder");
    }

    public async void InitializePublicClientAppForWAMBrokerAsync()
    {
        if (PublicClientApplicationBuilder != null)
        {
            PublicClientApplicationBuilder.WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows)
            {
                MsaPassthrough = true,
                Title = "Command Palette Azure Extension",
            });

            PublicClientApplication = PublicClientApplicationBuilder.Build();
            _log.Debug($"PublicClientApplication is instantiated");
        }
        else
        {
            _log.Error($"PublicClientApplicationBuilder is not initialized");
            throw new ArgumentNullException(null);
        }

        await TokenCacheRegistration(PublicClientApplication);
    }

    public async Task InitializePublicClientAppForWAMBrokerAsyncWithParentWindow()
    {
        if (PublicClientApplicationBuilder != null)
        {
            var windowHandle = Windows.Win32.PInvoke.FindWindow(null, "Microsoft Teams");
            var builder = PublicClientApplicationBuilder
                .WithParentActivityOrWindow(() => { return windowHandle; });

            builder = builder.WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows)
            {
                MsaPassthrough = true,
                Title = "Command Palette Azure Extension",
            });

            PublicClientApplication = PublicClientApplicationBuilder.Build();
            _log.Debug($"PublicClientApplication is instantiated for interactive UI capability");
        }
        else
        {
            _log.Error($"Incorrect parameters to instantiate PublicClientApplication with interactive UI capability");
            throw new ArgumentNullException(null);
        }

        await TokenCacheRegistration(PublicClientApplication);
    }

    private async Task<IEnumerable<IAccount>> TokenCacheRegistration(IPublicClientApplication publicClientApplication)
    {
        var storageProperties = new StorageCreationPropertiesBuilder(_microsoftEntraIdSettings.CacheFileName, _microsoftEntraIdSettings.CacheDir).Build();
        var msalCacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);
        msalCacheHelper.RegisterCache(publicClientApplication.UserTokenCache);
        _log.Debug($"Token cache is successfully registered with PublicClientApplication");

        // In the case the cache file is being reused there will be preexisting logged in accounts
        return await publicClientApplication.GetAccountsAsync().ConfigureAwait(false);
    }

    public async Task EnableSSOForAzureExtensionAsync()
    {
        // The below code will only acquire the default Window account token on first launch
        // i.e. token cache is empty and user has not explicitly signed out of the account
        // in the application.
        // If a user has explicitly signed out of the connected Windows account, WAM will
        // remember and not acquire a token on behalf of a user.
        _log.Debug($"Enable SSO for Azure Extension by connecting the Windows's default account");
        try
        {
            var accounts = await PublicClientApplication!.GetAccountsAsync().ConfigureAwait(false);
            if (!accounts.Any())
            {
                var silentTokenAcquisitionBuilder = PublicClientApplication.AcquireTokenSilent(_microsoftEntraIdSettings.ScopesArray, Microsoft.Identity.Client.PublicClientApplication.OperatingSystemAccount);
                if (Guid.TryParse(Microsoft.Identity.Client.PublicClientApplication.OperatingSystemAccount.HomeAccountId.TenantId, out var homeTenantId) && homeTenantId == MSATenetId)
                {
                    silentTokenAcquisitionBuilder = silentTokenAcquisitionBuilder.WithTenantId(TransferTenetId.ToString("D"));
                }

                await silentTokenAcquisitionBuilder.ExecuteAsync();
                _log.Information($"Azure SSO enabled");
            }
        }
        catch (Exception ex)
        {
            // This is best effort
            _log.Information($"Azure SSO failed with exception:{ex}");
        }
    }

    public IAccount GetDefaultAccount()
    {
        var accounts = PublicClientApplication!.GetAccountsAsync().Result;
        return accounts.FirstOrDefault()!;
    }

    public async Task<IEnumerable<IAccount>> GetLoggedInAccounts()
    {
        return await PublicClientApplication!.GetAccountsAsync();
    }

    public async Task<IAccount> ShowLogonSession()
    {
        await InitializePublicClientAppForWAMBrokerAsyncWithParentWindow();
        var account = await LoginDeveloperAccount();

        if (account == null)
        {
            _log.Error($"Invalid AuthRequest");
            throw new InvalidOperationException();
        }

        return account;
    }

    public async Task<IAccount?> LoginDeveloperAccount()
    {
        var authenticationResult = await InitiateAuthenticationFlowAsync();
        return authenticationResult?.Account;
    }

    private async Task<AuthenticationResult?> InitiateAuthenticationFlowAsync()
    {
        AuthenticationResult? authenticationResult = null;
        if (PublicClientApplication.IsUserInteractive() && PublicClientApplication != null)
        {
            try
            {
                authenticationResult = await PublicClientApplication.AcquireTokenInteractive(_microsoftEntraIdSettings.ScopesArray).ExecuteAsync();
            }
            catch (MsalClientException msalClientEx)
            {
                if (msalClientEx.ErrorCode == MsalError.AuthenticationCanceledError)
                {
                    _log.Information($"MSALClient: User canceled authentication:{msalClientEx}");
                }
                else
                {
                    _log.Error($"MSALClient: Error Acquiring Token:{msalClientEx}");
                }
            }
            catch (MsalException msalEx)
            {
                _log.Error($"MSAL: Error Acquiring Token:{msalEx}");
            }
            catch (Exception authenticationException)
            {
                _log.Error($"Authentication: Error Acquiring Token:{authenticationException}");
            }

            _log.Information($"MSAL: Signed in user by acquiring token interactively.");
        }

        return authenticationResult;
    }

    public async Task<bool> LogoutAccount(string username)
    {
        var accounts = await PublicClientApplication!.GetAccountsAsync().ConfigureAwait(false);

        foreach (var account in accounts)
        {
            if (account.Username == username)
            {
                await PublicClientApplication.RemoveAsync(account).ConfigureAwait(false);
                _log.Information($"MSAL: Signed out user.");
            }
            else
            {
                _log.Warning($"MSAL: User is already absent from cache .");
            }
        }

        return true;
    }

    public VssCredentials GetCredentials(IAccount account)
    {
        var authResult = ObtainTokenForLoggedInDeveloperAccount(account.Username).Result;
        return new VssAadCredential(new VssAadToken("Bearer", authResult.AccessToken));
    }

    public async Task<AuthenticationResult> ObtainTokenForLoggedInDeveloperAccount(string loginId)
    {
        _log.Debug($"ObtainTokenForLoggedInDeveloperAccount");

        var existingAccount = await GetDeveloperAccountFromCache(loginId);

        var silentTokenAcquisitionBuilder = PublicClientApplication!.AcquireTokenSilent(_microsoftEntraIdSettings.ScopesArray, existingAccount);
        if (Guid.TryParse(existingAccount!.HomeAccountId.TenantId, out var homeTenantId) && homeTenantId == MSATenetId)
        {
            silentTokenAcquisitionBuilder = silentTokenAcquisitionBuilder.WithTenantId(TransferTenetId.ToString("D"));
        }

        try
        {
            return await silentTokenAcquisitionBuilder.ExecuteAsync();
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
    }

    public async Task<IAccount?> GetDeveloperAccountFromCache(string loginId)
    {
        IEnumerable<IAccount> accounts = new List<IAccount>();

        if (PublicClientApplication != null)
        {
            accounts = await PublicClientApplication.GetAccountsAsync().ConfigureAwait(false);
        }

        foreach (var account in accounts)
        {
            if (account.Username == loginId)
            {
                return account;
            }
        }

        return null;
    }

    public bool IsSignedIn()
    {
        return GetLoggedInAccounts().Result.Any();
    }
}
