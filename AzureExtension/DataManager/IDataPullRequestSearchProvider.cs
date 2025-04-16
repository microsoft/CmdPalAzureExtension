// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls;
using PullRequestSearch = AzureExtension.DataModel.PullRequestSearch;

namespace AzureExtension.DataManager;

public interface IDataPullRequestSearchProvider
{
    PullRequestSearch? GetPullRequestSearch(IPullRequestSearch pullRequestSearch);

    IEnumerable<IPullRequest> GetPullRequests(IPullRequestSearch pullRequestSearch);
}
