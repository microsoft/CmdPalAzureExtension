// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Account;
using Microsoft.Identity.Client;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Serilog;

namespace AzureExtension.Client;

public class AzureClientProvider : IConnectionProvider, IDisposable
{
    private static readonly Lazy<ILogger> _logger = new(() => Serilog.Log.ForContext("SourceContext", nameof(AzureClientProvider)));

    private static readonly ILogger _log = _logger.Value;

    private readonly IAccountProvider _accountProvider;
    private readonly IVssConnectionFactory _factory;

    public AzureClientProvider(IAccountProvider accountProvider, IVssConnectionFactory factory)
    {
        _accountProvider = accountProvider;
        _factory = factory;
    }

    private IVssConnection CreateVssConnection(Uri uri, IAccount account)
    {
        var azureUri = new AzureUri(uri);
        if (!azureUri.IsValid)
        {
            _log.Information($"Cannot Create Connection: invalid uri argument value i.e. {uri}");
            throw new ArgumentException(uri.ToString());
        }

        var credentials = _accountProvider.GetCredentials(account);

        return _factory.CreateVssConnection(azureUri.Connection, credentials);
    }

    private async Task<IVssConnection> CreateVssConnectionAsync(Uri uri, IAccount account)
    {
        var azureUri = new AzureUri(uri);
        if (!azureUri.IsValid)
        {
            _log.Information($"Cannot Create Connection: invalid uri argument value i.e. {uri}");
            throw new ArgumentException(uri.ToString());
        }

        var credentials = await _accountProvider.GetCredentialsAsync(account);
        return _factory.CreateVssConnection(azureUri.Connection, credentials);
    }

    public async Task<ConnectionResult> CreateVssConnectionResult(Uri uri, IAccount account)
    {
        var azureUri = new AzureUri(uri);
        if (!azureUri.IsValid)
        {
            _log.Information($"Cannot Create Connection: invalid uri argument value i.e. {uri}");
            return new ConnectionResult(ResultType.Failure, ErrorType.InvalidArgument, false);
        }

        VssCredentials? credentials;
        try
        {
            credentials = await _accountProvider.GetCredentialsAsync(account);
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
            IVssConnection? connection = null;
            if (credentials != null)
            {
                connection = _factory.CreateVssConnection(azureUri.Connection, credentials);
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
    private IVssConnection GetVssConnection(Uri uri, IAccount account)
    {
        return CreateVssConnection(uri, account);
    }

    private bool IsConnectionExpired(IVssConnection connection)
    {
        try
        {
            var identity = connection.AuthorizedIdentity;
            return identity == null || identity.Id == Guid.Empty || !connection.HasAuthenticated;
        }
        catch (VssUnauthorizedException)
        {
            return true;
        }
        catch (Exception ex)
        {
            _log.Error($"Error checking if connection is expired: {ex}");
            return true;
        }
    }

    private readonly Dictionary<Tuple<Uri, IAccount>, IVssConnection> _connections = new();

    /// <summary>
    /// Gets the VssConnection. Not thread safe. Caches VssConnection for the same uri and account.
    /// </summary>
    /// <param name="uri">The uri to an Azure DevOps resource.</param>
    /// <param name="account">The developer to authenticate with.</param>
    /// <returns>An authorized connection to the resource.</returns>
    public async Task<IVssConnection> GetVssConnectionAsync(Uri uri, IAccount account)
    {
        var conectionKey = Tuple.Create(uri, account);

        if (_connections.TryGetValue(conectionKey, out var connection))
        {
            if (!IsConnectionExpired(connection))
            {
                return connection;
            }

            connection.Dispose();
            _connections.Remove(conectionKey);
        }

        var newConnection = await CreateVssConnectionAsync(uri, account);
        _connections.TryAdd(conectionKey, newConnection);
        return newConnection;
    }

    public Task<ConnectionResult> GetVssConnectionResult(Uri uri, IAccount account)
    {
        return CreateVssConnectionResult(uri, account);
    }

    public T GetClient<T>(string uri, IAccount account)
       where T : VssHttpClientBase
    {
        if (string.IsNullOrEmpty(uri))
        {
            throw new ArgumentException($"Cannot GetClient: invalid uri argument value i.e. {uri}");
        }

        var azureUri = new AzureUri(uri);
        if (!azureUri.IsValid)
        {
            throw new ArgumentException($"Cannot GetClient as uri validation failed: value of uri {uri}");
        }

        return GetClient<T>(azureUri.Connection, account);
    }

    public T GetClient<T>(Uri uri, IAccount account)
       where T : VssHttpClientBase
    {
        var connection = GetVssConnection(uri, account);
        if (connection == null)
        {
            throw new AzureClientException($"Cannot get connection for uri: value of uri {uri}");
        }

        return connection.GetClient<T>();
    }

    public Guid GetAuthorizedEntityId(Uri connection, IAccount account)
    {
        var vssConnection = GetVssConnection(connection, account);
        return vssConnection.AuthorizedIdentity.Id;
    }

    // Disposing area
    private bool _disposed;

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                foreach (var connection in _connections.Values)
                {
                    connection.Dispose();
                }
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
