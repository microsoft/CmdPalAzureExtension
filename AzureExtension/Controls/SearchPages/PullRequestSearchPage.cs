// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls.Commands;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Windows.Storage.Streams;

namespace AzureExtension.Controls.Pages;

public sealed partial class PullRequestSearchPage : SearchPage<IPullRequest>
{
    private readonly IPullRequestSearch _search;

    private readonly IResources _resources;

    private readonly IDataProvider _dataProvider;

    public PullRequestSearchPage(IPullRequestSearch search, IResources resources, IDataProvider dataProvider)
        : base(search, dataProvider)
    {
        _search = search;
        _resources = resources;
        _dataProvider = dataProvider;
        Icon = IconLoader.GetIcon("PullRequest");
        Name = search.Name;
        ShowDetails = true;
    }

    protected override ListItem GetListItem(IPullRequest item)
    {
        var title = item.Title;
        var url = item.HtmlUrl;

        return new ListItem(new LinkCommand(url, _resources))
        {
            Title = title,
            Icon = IconLoader.GetIconForPullRequestStatus(item.PolicyStatus),
            Details = new Details()
            {
                Title = item.Title,
                Metadata = new[]
                {
                    new DetailsElement()
                    {
                        Key = "Author:",
                        Data = new DetailsLink() { Text = $"{item.Creator?.Name}" },
                    },
                    new DetailsElement()
                    {
                        Key = "Last updated:",
                        Data = new DetailsLink() { Text = $"{item.Creator?.UpdatedAt}" },
                    },
                    new DetailsElement()
                    {
                        Key = "Target branch:",
                        Data = new DetailsLink() { Text = $"{item.TargetBranch}" },
                    },
                    new DetailsElement()
                    {
                        Key = "Policy status:",
                        Data = new DetailsLink() { Text = $"{item.PolicyStatus}" },
                    },
                    new DetailsElement()
                    {
                        Key = "Reason:",
                        Data = new DetailsLink() { Text = $"{item.PolicyStatusReason}" },
                    },
                    new DetailsElement()
                    {
                        Key = "Id:",
                        Data = new DetailsLink() { Text = $"{item.InternalId}" },
                    },
                    new DetailsElement()
                    {
                        Key = "Created:",
                        Data = new DetailsLink() { Text = $"{new DateTime(item.CreationDate)}" },
                    },
                },
            },
        };
    }

    protected override Task<IEnumerable<IPullRequest>> LoadContentData()
    {
        return _dataProvider.GetPullRequests(_search);
    }
}
