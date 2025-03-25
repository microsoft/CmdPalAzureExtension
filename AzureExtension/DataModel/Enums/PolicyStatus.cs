// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace AzureExtension.DataModel;

public enum PolicyStatus
{
    // Sorted by severity. Most severe is lowest.
    Broken = 1,
    Rejected = 2,
    Queued = 3,
    Running = 4,
    Approved = 5,
    NotApplicable = 6,
    Unknown = 7,
}
