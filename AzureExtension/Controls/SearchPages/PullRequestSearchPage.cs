// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls.Commands;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions.Toolkit;

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
            Subtitle = $"status: {item.Status}, policy status: {item.PolicyStatus}",

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
                        Key = "Avatar:",
                        Data = new DetailsLink() { Text = $"{item.Creator?.Avatar ?? "no string here"}" },
                    },
                    new DetailsElement()
                    {
                        Key = "Creator?.UpdatedAt",
                        Data = new DetailsLink() { Text = $"{item.Creator?.UpdatedAt}" },
                    },
                    new DetailsElement()
                    {
                        Key = "Creator?.DeveloperLoginId:",
                        Data = new DetailsLink() { Text = $"{item.Creator?.DeveloperLoginId}" },
                    },
                    new DetailsElement()
                    {
                        Key = "Creator?.TimeUpdated: ",
                        Data = new DetailsLink() { Text = $"{item.Creator?.TimeUpdated}" },
                    },
                    new DetailsElement()
                    {
                        Key = "Creator?.InternalId: ",
                        Data = new DetailsLink() { Text = $"{item.Creator?.InternalId}" },
                    },
                    new DetailsElement()
                    {
                        Key = "RepositoryId:",
                        Data = new DetailsLink() { Text = $"{item.RepositoryId}" },
                    },
                    new DetailsElement()
                    {
                        Key = "Pull request InternalId:",
                        Data = new DetailsLink() { Text = $"{item.InternalId}" },
                    },
                    new DetailsElement()
                    {
                        Key = "Url:",
                        Data = new DetailsLink() { Link = new Uri(item.Url) },
                    },
                    new DetailsElement()
                    {
                        Key = "HtmlUrl:",
                        Data = new DetailsLink() { Link = new Uri(item.HtmlUrl) },
                    },
                    new DetailsElement()
                    {
                        Key = "RepositoryGuid:",
                        Data = new DetailsLink() { Text = $"{item.RepositoryGuid}" },
                    },
                    new DetailsElement()
                    {
                        Key = "Status: ",
                        Data = new DetailsLink() { Text = $"{item.Status}" },
                    },
                    new DetailsElement()
                    {
                        Key = "Target branch:",
                        Data = new DetailsLink() { Text = $"{item.TargetBranch}" },
                    },
                    new DetailsElement()
                    {
                        Key = "Policy status: ",
                        Data = new DetailsLink() { Text = $"{item.PolicyStatus}" },
                    },
                    new DetailsElement()
                    {
                        Key = "Reason:",
                        Data = new DetailsLink() { Text = $"{item.PolicyStatusReason}" },
                    },
                    new DetailsElement()
                    {
                        Key = "Author:",
                        Data = new DetailsLink() { Text = $"{item.Creator}" },
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
