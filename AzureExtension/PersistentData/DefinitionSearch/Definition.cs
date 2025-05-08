// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls;

namespace AzureExtension.PersistentData;

public class Definition : IDefinition
{
    public long InternalId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string HtmlUrl { get; set; } = string.Empty;

    public IBuild? MostRecentBuild { get; set; }
}
