// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls;
using AzureExtension.DataManager.Cache;

namespace AzureExtension.DataManager;

public interface IDataProvider
{
    Task<IEnumerable<IWorkItem>> GetWorkItems(IQuery query);

    Task<IEnumerable<IPullRequest>> GetPullRequests(IPullRequestSearch pullRequestSearch);

    public event CacheManagerUpdateEventHandler? OnUpdate;
}
