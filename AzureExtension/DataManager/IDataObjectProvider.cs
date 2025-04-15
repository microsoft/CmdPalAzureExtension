// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls;
using PullRequestSearch = AzureExtension.DataModel.PullRequestSearch;
using Query = AzureExtension.DataModel.Query;

namespace AzureExtension.DataManager;

public interface IDataObjectProvider
{
    Query? GetQuery(IQuery query);

    PullRequestSearch? GetPullRequestSearch(IPullRequestSearch pullRequestSearch);

    IEnumerable<IWorkItem> GetWorkItems(IQuery query);

    IEnumerable<IPullRequest> GetPullRequests(IPullRequestSearch pullRequestSearch);
}
