// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Data;
using AzureExtension.DataModel;

namespace AzureExtension.Controls;

public interface IWorkItem
{
    string SystemTitle { get; }

    long Id { get; }

    string HtmlUrl { get; }

    string WorkItemTypeName { get; }

    string SystemState { get; }

    public string SystemReason { get; }

    public long SystemCreatedDate { get; }

    public long SystemChangedDate { get; }

    public Identity? SystemAssignedTo { get; }

    public Identity? SystemCreatedBy { get; }

    public Identity? SystemChangedBy { get; }
}
