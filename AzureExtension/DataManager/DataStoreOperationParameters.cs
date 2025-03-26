// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Client;
using AzureExtension.DataModel;

namespace AzureExtension;

public class DataStoreOperationParameters
{
    public IEnumerable<AzureUri> Uris { get; set; } = new List<AzureUri>();

    public string OperationName { get; set; } = string.Empty;

    public string LoginId { get; set; } = string.Empty;

    public Guid Requestor { get; set; }

    public DeveloperId.DeveloperId? DeveloperId { get; set; }

    public PullRequestView PullRequestView { get; set; } = PullRequestView.Unknown;

    public RequestOptions? RequestOptions { get; set; }

    public DataStoreOperationParameters()
    {
    }

    public override string ToString() => $"{OperationName} UriCount: {Uris.Count()} - {LoginId}";
}
