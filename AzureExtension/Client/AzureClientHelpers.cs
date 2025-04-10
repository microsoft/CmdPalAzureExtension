// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.DeveloperId;
using Microsoft.Identity.Client;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using Serilog;

namespace AzureExtension.Client;

public class AzureClientHelpers
{
    private readonly AzureClientProvider _azureClientProvider;

    public AzureClientHelpers(AzureClientProvider azureClientProvider)
    {
        _azureClientProvider = azureClientProvider;
    }

    // This validates the Query Uri authenticates and receives a response from the server.
    // It is used for validating an input Uri is actually valid to the server and adds information
    // about it from the server.
    public InfoResult GetQueryInfo(AzureUri azureUri, IAccount account)
    {
        var log = Log.ForContext("SourceContext", nameof(InfoResult));
        if (account == null)
        {
            return new InfoResult(azureUri, InfoType.Query, ResultType.Failure, ErrorType.NullDeveloperId);
        }

        if (string.IsNullOrEmpty(azureUri.ToString()))
        {
            return new InfoResult(azureUri, InfoType.Query, ResultType.Failure, ErrorType.EmptyUri);
        }

        if (!azureUri.IsQuery)
        {
            return new InfoResult(azureUri, InfoType.Query, ResultType.Failure, ErrorType.UriInvalidQuery);
        }

        try
        {
            var connectionResult = _azureClientProvider.GetVssConnection(azureUri.Connection, account);
            if (connectionResult.Result != ResultType.Success)
            {
                return new InfoResult(azureUri, InfoType.Query, ResultType.Failure, connectionResult.Error, connectionResult.Exception);
            }

            var witClient = connectionResult.Connection!.GetClient<WorkItemTrackingHttpClient>();
            if (witClient == null)
            {
                return new InfoResult(azureUri, InfoType.Query, ResultType.Failure, ErrorType.FailedGettingClient);
            }

            var getQueryResult = witClient.GetQueryAsync(azureUri.Project, azureUri.Query).Result;
            if (getQueryResult == null)
            {
                return new InfoResult(azureUri, InfoType.Query, ResultType.Failure, ErrorType.QueryFailed);
            }

            return new InfoResult(azureUri, InfoType.Query, getQueryResult.Name, getQueryResult.Path);
        }
        catch (Exception ex)
        {
            if (ex.InnerException is VssResourceNotFoundException)
            {
                log.Error(ex, $"Vss Resource Not Found for {azureUri}");
                return new InfoResult(azureUri, InfoType.Query, ResultType.Failure, ErrorType.VssResourceNotFound, ex);
            }
            else
            {
                log.Error(ex, $"Failed getting query info for: {azureUri}");
                return new InfoResult(azureUri, InfoType.Query, ResultType.Failure, ErrorType.Unknown, ex);
            }
        }
    }

    public InfoResult GetQueryInfo(string uri, IAccount account)
    {
        return GetQueryInfo(new AzureUri(uri), account);
    }

    public InfoResult GetQueryInfo(Uri uri, IAccount account)
    {
        return GetQueryInfo(new AzureUri(uri), account);
    }

    // This validates the Repository Uri authenticates and receives a response from the server.
    // It is used for validating an input Uri is actually valid to the server and adds information
    // about the target repository from the server.
    public InfoResult GetRepositoryInfo(AzureUri azureUri, IAccount account)
    {
        var log = Log.ForContext("SourceContext", nameof(InfoResult));
        if (account == null)
        {
            return new InfoResult(azureUri, InfoType.Repository, ResultType.Failure, ErrorType.NullDeveloperId);
        }

        if (string.IsNullOrEmpty(azureUri.ToString()))
        {
            return new InfoResult(azureUri, InfoType.Repository, ResultType.Failure, ErrorType.EmptyUri);
        }

        if (!azureUri.IsRepository)
        {
            return new InfoResult(azureUri, InfoType.Repository, ResultType.Failure, ErrorType.UriInvalidRepository);
        }

        try
        {
            var connectionResult = _azureClientProvider.GetVssConnection(azureUri.Connection, account);
            if (connectionResult.Result != ResultType.Success)
            {
                return new InfoResult(azureUri, InfoType.Repository, ResultType.Failure, connectionResult.Error, connectionResult.Exception);
            }

            var gitClient = connectionResult.Connection!.GetClient<GitHttpClient>();
            if (gitClient == null)
            {
                return new InfoResult(azureUri, InfoType.Repository, ResultType.Failure, ErrorType.FailedGettingClient);
            }

            var getRepositoryResult = gitClient.GetRepositoryAsync(azureUri.Project, azureUri.Repository).Result;
            if (getRepositoryResult == null)
            {
                return new InfoResult(azureUri, InfoType.Repository, ResultType.Failure, ErrorType.RepositoryFailed);
            }

            return new InfoResult(azureUri, InfoType.Repository, getRepositoryResult.Name, getRepositoryResult.Id.ToString());
        }
        catch (Exception ex)
        {
            if (ex.InnerException is VssResourceNotFoundException)
            {
                log.Error(ex, $"Vss Resource Not Found for {azureUri}");
                return new InfoResult(azureUri, InfoType.Repository, ResultType.Failure, ErrorType.VssResourceNotFound, ex);
            }
            else
            {
                log.Error(ex, $"Failed getting repository info for: {azureUri}");
                return new InfoResult(azureUri, InfoType.Repository, ResultType.Failure, ErrorType.Unknown, ex);
            }
        }
    }

    public InfoResult GetRepositoryInfo(string uri, IAccount account)
    {
        return GetRepositoryInfo(new AzureUri(uri), account);
    }

    public InfoResult GetRepositoryInfo(Uri uri, IAccount account)
    {
        return GetRepositoryInfo(new AzureUri(uri), account);
    }
}
