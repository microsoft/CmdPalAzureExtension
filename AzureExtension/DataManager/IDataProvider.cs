// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls;

namespace AzureExtension.DataManager;

public interface IDataProvider
{
    Task<IEnumerable<WorkItem>> GetWorkItems(IQuery query);

    Task<IEnumerable<PullRequest>> GetPullRequests(PullRequestSearch pullRequestSearch);
}
