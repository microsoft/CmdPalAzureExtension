// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace AzureExtension.Controls;

public interface IDefinitionSearch
{
    int InternalId { get; set; } // This is the ID of the definition in Azure DevOps

    string ProjectUrl { get; set; } // This is the URL of the project in Azure DevOps
}
