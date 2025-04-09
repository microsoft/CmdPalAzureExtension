// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Client;

namespace AzureExtension.Controls
{
    public class QueryObject
    {
        public AzureUri AzureUri { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public QueryObject()
        {
            AzureUri = new AzureUri();
            Name = string.Empty;
            Description = string.Empty;
        }

        public QueryObject(AzureUri azureUri, string name, string description)
        {
            AzureUri = azureUri;
            Name = name;
            Description = description;
        }
    }
}
