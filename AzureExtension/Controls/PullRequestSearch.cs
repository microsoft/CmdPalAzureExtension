// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Client;

namespace AzureExtension.Controls;

public class PullRequestSearch : IPullRequestSearch
{
    public string Name { get; set; } = string.Empty;

    public string Url => AzureUri.OriginalString;

    public AzureUri AzureUri { get; set; }

    public string View { get; set; } = string.Empty;

    public string PullRequestUrl { get; set; } = string.Empty;

    public bool IsTopLevel { get; set; }

    public PullRequestSearch()
    {
        AzureUri = new AzureUri();
        Name = string.Empty;
    }

    public PullRequestSearch(AzureUri azureUri, string title, string view, bool isTopLevel)
    {
        AzureUri = azureUri;
        Name = title;
        View = view;
        PullRequestUrl = azureUri.Uri.ToString();
        IsTopLevel = isTopLevel;
    }
}
