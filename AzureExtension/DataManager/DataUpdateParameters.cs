// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace AzureExtension.DataManager;

public enum DataUpdateType
{
    All,
    Query,
    PullRequests,
    Pipeline,
    Repository,
}

public class DataUpdateParameters
{
    public CancellationToken? CancellationToken { get; set; }

    public DataUpdateType UpdateType { get; set; }

    public object? UpdateObject { get; set; }
}
