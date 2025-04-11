// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Account;
using Microsoft.Identity.Client;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Serilog;

namespace AzureExtension.Client;

public class AzureClientProvider
{
    private static readonly Lazy<ILogger> _logger = new(() => Serilog.Log.ForContext("SourceContext", nameof(AzureClientProvider)));

    private static readonly ILogger _log = _logger.Value;

    private readonly IAccountProvider _accountProvider;

    public AzureClientProvider(IAccountProvider accountProvider)
    {
        _accountProvider = accountProvider;
    }

    private VssConnection? CreateConnection(Uri uri, IAccount account)
    {
        var azureUri = new AzureUri(uri);
        if (!azureUri.IsValid)
        {
            _log.Information($"Cannot Create Connection: invalid uri argument value i.e. {uri}");
            return null;
        }

        if (account == null)
        {
            _log.Information($"Cannot Create Connection: null developer id argument");
            return null;
        }

        try
        {
            VssCredentials? credentials;
            try
            {
                credentials = _accountProvider.GetCredentials(account);

                if (credentials == null)
                {
                    _log.Error($"Unable to get credentials for developerId");
                    return null;
                }
            }
            catch (MsalUiRequiredException ex)
            {
                _log.Error($"Unable to get credentials for developerId failed and requires user interaction {ex}");
                return null;
            }
            catch (MsalServiceException ex)
            {
                _log.Error($"Unable to get credentials for developerId: failed with MSAL service error: {ex}");
                return null;
            }
            catch (MsalClientException ex)
            {
                _log.Error($"Unable to get credentials for developerId: failed with MSAL client error: {ex}");
                return null;
            }
            catch (Exception ex)
            {
                _log.Error($"Unable to get credentials for developerId {ex}");
                return null;
            }

            var connection = new VssConnection(azureUri.Connection, credentials);
            if (connection != null)
            {
                _log.Debug($"Connection created for developer id");
                return connection;
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed creating connection for developer id and {uri} with exception:");
        }

        return null;
    }

    public ConnectionResult CreateVssConnection(Uri uri, IAccount account)
    {
        var azureUri = new AzureUri(uri);
        if (!azureUri.IsValid)
        {
            _log.Information($"Cannot Create Connection: invalid uri argument value i.e. {uri}");
            return new ConnectionResult(ResultType.Failure, ErrorType.InvalidArgument, false);
        }

        if (account == null)
        {
            _log.Information($"Cannot Create Connection: invalid developer id argument");
            return new ConnectionResult(ResultType.Failure, ErrorType.InvalidArgument, false);
        }

        VssCredentials? credentials;
        try
        {
            credentials = _accountProvider.GetCredentials(account);
            if (credentials == null)
            {
                _log.Error($"Unable to get credentials for developerId");
                return new ConnectionResult(ResultType.Failure, ErrorType.InvalidDeveloperId, false);
            }
        }
        catch (MsalUiRequiredException ex)
        {
            _log.Error($"AcquireDeveloperAccountToken failed and requires user interaction {ex}");
            return new ConnectionResult(ResultType.Failure, ErrorType.CredentialUIRequired, false, ex);
        }
        catch (MsalServiceException ex)
        {
            _log.Error($"AcquireDeveloperAccountToken failed with MSAL service error: {ex}");
            return new ConnectionResult(ResultType.Failure, ErrorType.MsalServiceError, false, ex);
        }
        catch (MsalClientException ex)
        {
            _log.Error($"AcquireDeveloperAccountToken failed with MSAL client error: {ex}");
            return new ConnectionResult(ResultType.Failure, ErrorType.MsalClientError, false, ex);
        }
        catch (Exception ex)
        {
            _log.Error($"AcquireDeveloperAccountToken failed with error: {ex}");
            return new ConnectionResult(ResultType.Failure, ErrorType.GenericCredentialFailure, true);
        }

        try
        {
            VssConnection? connection = null;
            if (credentials != null)
            {
                connection = new VssConnection(azureUri.Connection, credentials);
            }

            if (connection != null)
            {
                _log.Debug($"Created new connection to {azureUri.Connection} for {account.Username}");
                return new ConnectionResult(azureUri.Connection, null, connection);
            }
            else
            {
                _log.Error($"Connection to {azureUri.Connection} was null.");
                return new ConnectionResult(ResultType.Failure, ErrorType.NullConnection, false);
            }
        }
        catch (Exception ex)
        {
            _log.Error($"Unable to establish VssConnection: {ex}");
            return new ConnectionResult(ResultType.Failure, ErrorType.InitializeVssConnectionFailure, true, ex);
        }
    }

    /// <summary>
    /// Gets the Azure DevOps connection for the specified developer id.
    /// </summary>
    /// <param name="uri">The uri to an Azure DevOps resource.</param>
    /// <param name="account">The developer to authenticate with.</param>
    /// <returns>An authorized connection to the resource.</returns>
    /// <exception cref="ArgumentException">If the azure uri is not valid.</exception>
    /// <exception cref="ArgumentNullException">If developerId is null.</exception>
    /// <exception cref="AzureClientException">If a connection can't be made.</exception>
    public VssConnection GetConnection(Uri uri, IAccount account)
    {
        var azureUri = new AzureUri(uri);
        if (!azureUri.IsValid)
        {
            _log.Error($"Uri is an invalid Azure Uri: {uri}");
            throw new ArgumentException(uri.ToString());
        }

        if (account == null)
        {
            _log.Error($"No logged in developer for which connection needs to be retrieved");
            throw new ArgumentNullException(null);
        }

        var connection = CreateConnection(azureUri.Connection, account);
        if (connection == null)
        {
            _log.Error($"Failed creating connection for developer id");
            throw new AzureClientException($"Failed creating Vss connection: {azureUri.Connection} for {account.Username}");
        }

        return connection;
    }

    public ConnectionResult GetVssConnection(Uri uri, IAccount account)
    {
        if (account == null)
        {
            _log.Error($"No logged in developer for which connection needs to be retrieved");
            return new ConnectionResult(ResultType.Failure, ErrorType.InvalidDeveloperId, false);
        }

        return CreateVssConnection(uri, account);
    }

    public T? GetClient<T>(string uri, IAccount account)
       where T : VssHttpClientBase
    {
        if (string.IsNullOrEmpty(uri))
        {
            _log.Information($"Cannot GetClient: invalid uri argument value i.e. {uri}");
            return null;
        }

        var azureUri = new AzureUri(uri);
        if (!azureUri.IsValid)
        {
            _log.Information($"Cannot GetClient as uri validation failed: value of uri {uri}");
            return null;
        }

        return GetClient<T>(azureUri.Connection, account);
    }

    public T? GetClient<T>(Uri uri, IAccount account)
       where T : VssHttpClientBase
    {
        var connection = GetConnection(uri, account);
        if (connection == null)
        {
            return null;
        }

        return connection.GetClient<T>();
    }

    public ConnectionResult GetAzureDevOpsClient<T>(string uri, IAccount account)
        where T : VssHttpClientBase
    {
        var azureUri = new AzureUri(uri);
        if (!azureUri.IsValid)
        {
            _log.Information($"Cannot GetClient as uri validation failed: value of uri {uri}");
            return new ConnectionResult(ResultType.Failure, ErrorType.InvalidArgument, false);
        }

        return GetAzureDevOpsClient<T>(azureUri.Uri, account);
    }

    public ConnectionResult GetAzureDevOpsClient<T>(Uri uri, IAccount account)
       where T : VssHttpClientBase
    {
        var connectionResult = GetVssConnection(uri, account);
        if (connectionResult.Result == ResultType.Failure)
        {
            return connectionResult;
        }

        if (connectionResult.Connection != null)
        {
            return new ConnectionResult(uri, connectionResult.Connection.GetClient<T>(), connectionResult.Connection);
        }

        return new ConnectionResult(ResultType.Failure, ErrorType.FailedGettingClient, false);
    }
}
