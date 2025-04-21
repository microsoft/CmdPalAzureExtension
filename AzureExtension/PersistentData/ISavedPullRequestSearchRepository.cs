// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls;

namespace AzureExtension.PersistentData;

public interface ISavedPullRequestSearchRepository : IAzureSearchRepository
{
    Task AddSavedPullRequestSearch(IPullRequestSearch pullRequestSearch);

    Task RemoveSavedPullRequestSearch(IPullRequestSearch pullRequestSearch);

    IPullRequestSearch GetPullRequestSearch(string title, string url, string view);

    Task<IEnumerable<IPullRequestSearch>> GetSavedPullRequestSearches();

    Task<IEnumerable<IPullRequestSearch>> GetTopLevelPullRequestSearches();

    Task<bool> IsTopLevel(IPullRequestSearch pullRequestSearch);

    void UpdatePullRequestSearchTopLevelStatus(IPullRequestSearch pullRequestSearch, bool isTopLevel);
}
