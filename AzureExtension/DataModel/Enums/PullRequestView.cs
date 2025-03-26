// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace AzureExtension.DataModel;

public enum PullRequestView
{
    Unknown,

    /// <summary>
    /// View all pull requests.
    /// </summary>
    All,

    /// <summary>
    /// View my pull requests.
    /// </summary>
    Mine,

    /// <summary>
    /// View pull requests assigned to me.
    /// </summary>
    Assigned,
}
