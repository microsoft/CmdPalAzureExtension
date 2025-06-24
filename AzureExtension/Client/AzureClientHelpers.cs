// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Identity.Client;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using Serilog;

namespace AzureExtension.Client;

public class AzureClientHelpers
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(AzureClientHelpers));
    private readonly AzureClientProvider _azureClientProvider;

    public AzureClientHelpers(AzureClientProvider azureClientProvider)
    {
        _azureClientProvider = azureClientProvider;
    }

    // This validates the Query Uri authenticates and receives a response from the server.
    // It is used for validating an input Uri is actually valid to the server and adds information
    // about it from the server.
    private async Task<InfoResult> GetQueryInfo(AzureUri azureUri, IVssConnection vssConnection)
    {
        var witClient = vssConnection.GetClient<WorkItemTrackingHttpClient>();
        if (witClient == null)
        {
            return new InfoResult(azureUri, InfoType.Query, ResultType.Failure, ErrorType.FailedGettingClient);
        }

        var getQueryResult = await witClient.GetQueryAsync(azureUri.Project, azureUri.Query);
        if (getQueryResult == null)
        {
            return new InfoResult(azureUri, InfoType.Query, ResultType.Failure, ErrorType.QueryFailed);
        }

        return new InfoResult(azureUri, InfoType.Query, getQueryResult.Name, getQueryResult.Path);
    }

    // This validates the Repository Uri authenticates and receives a response from the server.
    // It is used for validating an input Uri is actually valid to the server and adds information
    // about the target repository from the server.
    private async Task<InfoResult> GetRepositoryInfo(AzureUri azureUri, IVssConnection vssConnection)
    {
        var gitClient = vssConnection.GetClient<GitHttpClient>();
        if (gitClient == null)
        {
            return new InfoResult(azureUri, InfoType.Repository, ResultType.Failure, ErrorType.FailedGettingClient);
        }

        var getRepositoryResult = await gitClient.GetRepositoryAsync(azureUri.Project, azureUri.Repository);
        if (getRepositoryResult == null)
        {
            return new InfoResult(azureUri, InfoType.Repository, ResultType.Failure, ErrorType.RepositoryFailed);
        }

        return new InfoResult(azureUri, InfoType.Repository, getRepositoryResult.Name, getRepositoryResult.Id.ToString());
    }

    private async Task<InfoResult> GetDefinitionInfo(AzureUri azureUri, long definitionId, IVssConnection vssConnection)
    {
        var buildClient = vssConnection.GetClient<BuildHttpClient>();
        if (buildClient == null)
        {
            return new InfoResult(azureUri, InfoType.Project, ResultType.Failure, ErrorType.FailedGettingClient);
        }

        var getDefinitionResult = await buildClient.GetDefinitionAsync(azureUri.Project, (int)definitionId);
        if (getDefinitionResult == null)
        {
            return new InfoResult(azureUri, InfoType.Project, ResultType.Failure, ErrorType.DefinitionNotFound);
        }

        return new InfoResult(azureUri, InfoType.Project, getDefinitionResult.Name, $"{getDefinitionResult.Id}");
    }

    // This last argument is not ideal, but it is used to pass the definitionId.
    // Might be better to pass the definitionId in the AzureUri, but requires more refactoring.
    public async Task<InfoResult> GetInfo(AzureUri azureUri, IAccount account, InfoType infoType, long? definitionId = null)
    {
        if (account == null)
        {
            return new InfoResult(azureUri, infoType, ResultType.Failure, ErrorType.NullDeveloperId);
        }

        if (string.IsNullOrEmpty(azureUri.ToString()))
        {
            return new InfoResult(azureUri, infoType, ResultType.Failure, ErrorType.EmptyUri);
        }

        if (!azureUri.IsValid)
        {
            return new InfoResult(azureUri, infoType, ResultType.Failure, ErrorType.InvalidUri);
        }

        if (infoType == InfoType.Query && azureUri.IsTempQuery)
        {
            return new InfoResult(azureUri, infoType, ResultType.Failure, ErrorType.TemporaryQueryUriNotSupported);
        }

        if (infoType == InfoType.Query && !azureUri.IsQuery)
        {
            return new InfoResult(azureUri, infoType, ResultType.Failure, ErrorType.InvalidQueryUri);
        }

        if (infoType == InfoType.Repository && !azureUri.Uri.AbsoluteUri.Contains("_git/"))
        {
            return new InfoResult(azureUri, infoType, ResultType.Failure, ErrorType.InvalidRepositoryUri);
        }

        if (infoType == InfoType.Definition && !azureUri.Uri.AbsoluteUri.Contains("_build?definitionId=") && definitionId == null)
        {
            return new InfoResult(azureUri, infoType, ResultType.Failure, ErrorType.InvalidDefinitionUri);
        }

        try
        {
            var connectionResult = await _azureClientProvider.GetVssConnectionResult(azureUri.Connection, account);
            if (connectionResult.Result != ResultType.Success)
            {
                return new InfoResult(azureUri, infoType, ResultType.Failure, connectionResult.Error, connectionResult.Exception);
            }

            return infoType switch
            {
                InfoType.Query => await GetQueryInfo(azureUri, connectionResult.Connection!),
                InfoType.Repository => await GetRepositoryInfo(azureUri, connectionResult.Connection!),
                InfoType.Definition => await GetDefinitionInfo(azureUri, (long)definitionId!, connectionResult.Connection!),
                _ => new InfoResult(azureUri, infoType, ResultType.Failure, ErrorType.Unknown),
            };
        }
        catch (Exception ex)
        {
            if (ex.InnerException is VssResourceNotFoundException)
            {
                _log.Error(ex, $"Vss Resource Not Found for {azureUri}");
                return new InfoResult(azureUri, infoType, ResultType.Failure, ErrorType.VssResourceNotFound, ex);
            }
            else
            {
                _log.Error(ex, $"Failed getting info for: {azureUri}");
                return new InfoResult(azureUri, infoType, ResultType.Failure, ErrorType.Unknown, ex);
            }
        }
    }

    public Task<InfoResult> GetInfo(string url, IAccount account, InfoType infoType, long? definitionId = null)
    {
        var azureUri = new AzureUri(url);
        return GetInfo(azureUri, account, infoType, definitionId);
    }
}
