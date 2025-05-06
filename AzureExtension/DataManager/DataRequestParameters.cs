// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace AzureExtension.DataManager;

public enum DataRequestType
{
    Query,
    PullRequests,
    Builds,
    Definition,
}

public class DataRequestParameters
{
    public DataRequestType RequestType { get; set; }

    public object? RequestObject { get; set; }
}
