// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace AzureExtension.Controls;

public interface IDefinition
{
    long InternalId { get; set; } // This is the ID of the definition in Azure DevOps

    string Name { get; set; } // The name of the definition

    string HtmlUrl { get; set; } // The URL of the definition in Azure DevOps

    IBuild? MostRecentBuild { get; } // The most recent build associated with this definition
}
