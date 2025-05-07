// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Client;
using Microsoft.Identity.Client;

namespace AzureExtension.PersistentData;

public interface IAzureValidator
{
    Task<InfoResult> GetQueryInfo(string queryUrl, IAccount account);

    Task<InfoResult> GetRepositoryInfo(string repositoryUrl, IAccount account);

    Task<InfoResult> GetDefinitionInfo(string searchUrl, long definitionId, IAccount account);
}
