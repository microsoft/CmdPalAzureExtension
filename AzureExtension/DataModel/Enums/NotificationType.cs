// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace AzureExtension.DataModel;

public enum NotificationType
{
    Unknown = 0,
    PullRequestApproved = 1,
    PullRequestRejected = 2,
    NewReview = 3,
}
