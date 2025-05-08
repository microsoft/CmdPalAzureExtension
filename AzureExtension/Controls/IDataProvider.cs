// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.DataManager.Cache;

namespace AzureExtension.Controls;

public interface IDataProvider
{
    Task<IEnumerable<IWorkItem>> GetWorkItems(IQuery query);

    Task<IEnumerable<IPullRequest>> GetPullRequests(IPullRequestSearch pullRequestSearch);

    Task<IEnumerable<IBuild>> GetBuilds(IPipelineDefinitionSearch definitionSearch);

    Task<IDefinition> GetDefinition(IPipelineDefinitionSearch definitionSearch);

    event CacheManagerUpdateEventHandler? OnUpdate;
}
