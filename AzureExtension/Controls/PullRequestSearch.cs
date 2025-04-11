// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Client;
using AzureExtension.DataModel;

namespace AzureExtension.Controls
{
    public class PullRequestSearch
    {
        public string Title { get; set; } = string.Empty;

        public string Url => AzureUri.OriginalString;

        public AzureUri AzureUri { get; set; }

        public long Id { get; set; }

        public long RepositoryId { get; set; }

        public string Status { get; set; } = string.Empty;

        public string PolicyStatus { get; set; } = string.Empty;

        public string PolicyStatusReason { get; set; } = string.Empty;

        public Identity? Creator { get; set; }

        public string TargetBranch { get; set; } = string.Empty;

        public long CreationDate { get; set; }

        public string View { get; set; } = string.Empty;

        public PullRequestSearch()
        {
            AzureUri = new AzureUri();
            Title = string.Empty;
        }

        public PullRequestSearch(AzureUri azureUri, string title, string view)
        {
            AzureUri = azureUri;
            Title = title;
            View = view;
        }
    }
}
