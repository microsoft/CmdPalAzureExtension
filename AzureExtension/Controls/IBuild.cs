// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.DataModel;

namespace AzureExtension.Controls;

public interface IBuild
{
    long InternalId { get; set; }

    string BuildNumber { get; set; }

    string Status { get; set; }

    string Result { get; set; }

    string SourceBranch { get; set; }

    Identity? Requester { get; }

    string Url { get; set; } // The URL of the build in Azure DevOps
}
