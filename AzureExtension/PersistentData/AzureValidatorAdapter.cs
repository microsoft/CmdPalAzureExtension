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
}
