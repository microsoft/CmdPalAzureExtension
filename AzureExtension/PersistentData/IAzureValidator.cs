// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Client;
using Microsoft.Identity.Client;

namespace AzureExtension.PersistentData;

public interface IAzureValidator
{
    InfoResult GetQueryInfo(string queryUrl, IAccount account);

    InfoResult GetRepositoryInfo(string repositoryUrl, IAccount account);

    InfoResult GetDefinitionInfo(string searchUrl, long definitionId, IAccount account);
}
