// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Client;

namespace AzureExtension.Controls;

public class Query : IQuery
{
    public AzureUri AzureUri { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Url => AzureUri.OriginalString;

    public string Description { get; set; } = string.Empty;

    public bool IsTopLevel { get; set; }

    public Query()
    {
        AzureUri = new AzureUri();
        Name = string.Empty;
        Description = string.Empty;
        IsTopLevel = false;
    }

    public Query(AzureUri azureUri, string name, string description, bool isTopLevel)
    {
        AzureUri = azureUri;
        Name = name;
        Description = description;
        IsTopLevel = isTopLevel;
    }
}
