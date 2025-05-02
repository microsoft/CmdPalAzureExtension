// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.DataModel;

namespace AzureExtension.Controls;

public interface IWorkItem
{
    string SystemTitle { get; }

    long Id { get; }

    long InternalId { get; }

    string HtmlUrl { get; }

    string WorkItemTypeName { get; }

    string SystemState { get; }

    string SystemReason { get; }

    long SystemCreatedDate { get; }

    long SystemChangedDate { get; }

    Identity? SystemAssignedTo { get; }

    Identity? SystemCreatedBy { get; }

    Identity? SystemChangedBy { get; }
}
