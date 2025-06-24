// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Client;

namespace AzureExtension.Controls.Forms;

public abstract class SearchInfoParameters
{
    public string Url { get; }

    public InfoType InfoType { get; }

    protected SearchInfoParameters(string url, InfoType infoType)
    {
        Url = url;
        InfoType = infoType;
    }
}

// Currently used for queries and pull request searches
#pragma warning disable SA1402 // File may only contain a single type
public class DefaultSearchInfoParameters : SearchInfoParameters
#pragma warning restore SA1402 // File may only contain a single type
{
    public DefaultSearchInfoParameters(string url, InfoType infoType)
        : base(url, infoType)
    {
    }
}

#pragma warning disable SA1402 // File may only contain a single type
public class DefinitionInfoParameters : SearchInfoParameters
#pragma warning restore SA1402 // File may only contain a single type
{
    public long DefinitionId { get; }

    public DefinitionInfoParameters(string url, long definitionId)
        : base(url, InfoType.Definition)
    {
        DefinitionId = definitionId;
    }
}
