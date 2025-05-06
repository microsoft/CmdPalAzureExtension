// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Client;
using Microsoft.Identity.Client;

namespace AzureExtension.PersistentData;

public class AzureValidatorAdapter : IAzureValidator
{
    private readonly AzureClientHelpers _azureClientHelpers;

    public AzureValidatorAdapter(AzureClientHelpers azureClientHelpers)
    {
        _azureClientHelpers = azureClientHelpers;
    }

    public async Task<InfoResult> GetQueryInfo(string queryUrl, IAccount account)
    {
        var queryInfo = await _azureClientHelpers.GetInfo(queryUrl, account, InfoType.Query);
        if (queryInfo.Result != ResultType.Success)
        {
            throw new InvalidOperationException(queryInfo.ErrorMessage);
        }

        return queryInfo;
    }

    public async Task<InfoResult> GetRepositoryInfo(string repositoryUrl, IAccount account)
    {
        var repositoryInfo = await _azureClientHelpers.GetInfo(repositoryUrl, account, InfoType.Repository);
        if (repositoryInfo.Result != ResultType.Success)
        {
            throw new InvalidOperationException(repositoryInfo.ErrorMessage);
        }

        return repositoryInfo;
    }

    public async Task<InfoResult> GetDefinitionInfo(string searchUrl, long definitionId, IAccount account)
    {
        var searchInfo = await _azureClientHelpers.GetInfo(searchUrl, account, InfoType.Definition, definitionId);
        if (searchInfo.Result != ResultType.Success)
        {
            throw new InvalidOperationException(searchInfo.ErrorMessage);
        }

        return searchInfo;
    }
}
