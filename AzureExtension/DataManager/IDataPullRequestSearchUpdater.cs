// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls;

namespace AzureExtension.DataManager;

public interface IDataPullRequestSearchUpdater
{
    bool IsNewOrStale(IPullRequestSearch pullRequestSearch, TimeSpan refreshCooldown);

    Task UpdatePullRequestsAsync(IPullRequestSearch pullRequestSearch, CancellationToken cancellationToken);
}
