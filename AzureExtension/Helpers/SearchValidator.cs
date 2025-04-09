// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Client;
using AzureExtension.DeveloperId;

namespace AzureExtension.Helpers
{
    public static class SearchValidator
    {
        public static InfoResult GetQueryInfo(string queryUrl, string queryName, IDeveloperId developerId)
        {
            if (string.IsNullOrEmpty(queryUrl))
            {
                throw new InvalidOperationException("Query URL or name cannot be null or empty.");
            }

            var queryInfo = AzureClientHelpers.GetQueryInfo(queryUrl, developerId);
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
}
