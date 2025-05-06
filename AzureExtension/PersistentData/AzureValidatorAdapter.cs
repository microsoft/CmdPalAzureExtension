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

    public InfoResult GetQueryInfo(string queryUrl, IAccount account)
    {
        if (string.IsNullOrEmpty(queryUrl))
        {
            throw new InvalidOperationException("Query URL or name cannot be null or empty.");
        }

        var queryInfo = _azureClientHelpers.GetQueryInfo(queryUrl, account);
        if (queryInfo.Result != ResultType.Success)
        {
            throw new InvalidOperationException(queryInfo.ErrorMessage);
        }
        else
        {
            return queryInfo;
        }
    }

    public InfoResult GetRepositoryInfo(string repositoryUrl, IAccount account)
    {
        if (string.IsNullOrEmpty(repositoryUrl))
        {
            throw new InvalidOperationException("Repository URL cannot be null or empty.");
        }

        var repositoryInfo = _azureClientHelpers.GetRepositoryInfo(repositoryUrl, account);
        if (repositoryInfo.Result != ResultType.Success)
        {
            throw new InvalidOperationException(repositoryInfo.ErrorMessage);
        }
        else
        {
            return repositoryInfo;
        }
    }

    public InfoResult GetDefinitionInfo(string searchUrl, long definitionId, IAccount account)
    {
        if (string.IsNullOrEmpty(searchUrl))
        {
            throw new InvalidOperationException("Search URL cannot be null or empty.");
        }

        var searchInfo = _azureClientHelpers.GetDefinitionInfo(searchUrl, definitionId, account);
        if (searchInfo.Result != ResultType.Success)
        {
            throw new InvalidOperationException(searchInfo.ErrorMessage);
        }
        else
        {
            return searchInfo;
        }
    }
}
