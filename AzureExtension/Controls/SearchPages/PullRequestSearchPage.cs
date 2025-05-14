// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls.Commands;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Pages;

public sealed partial class PullRequestSearchPage : SearchPage<IPullRequest>
{
    private readonly IResources _resources;
    private readonly TimeSpanHelper _timeSpanHelper;

    public PullRequestSearchPage(
        IPullRequestSearch search,
        IResources resources,
        ILiveContentDataProvider<IPullRequest> contentDataProvider,
        TimeSpanHelper timeSpanHelper)
        : base(search, contentDataProvider)
    {
        _resources = resources;
        _timeSpanHelper = timeSpanHelper;
        Icon = IconLoader.GetIcon("PullRequest");
        Name = search.Name;
        ShowDetails = true;
    }

    protected override ListItem GetListItem(IPullRequest item)
    {
        var title = item.Title;
        var url = item.HtmlUrl;

        return new ListItem(new LinkCommand(url, _resources, null))
        {
            Title = title,
            Icon = IconLoader.GetIconForPullRequestStatus(item.PolicyStatus),
            MoreCommands = new CommandContextItem[]
            {
                new(new CopyCommand(item.InternalId.ToStringInvariant(), _resources.GetResource("Pages_PullRequestSearchPage_CopyIdCommand"))),
                new(new CopyCommand(item.HtmlUrl, _resources.GetResource("Pages_PullRequestSearchPage_CopyURLCommand"))),
            },
            Details = new Details()
            {
                Title = item.Title,
                Metadata = new[]
                {
                    new DetailsElement()
                    {
                        Key = _resources.GetResource("Pages_PullRequestSearchPage_Author"),
                        Data = new DetailsLink() { Text = $"{item.Creator?.Name}" },
                    },
                    new DetailsElement()
                    {
                        Key = _resources.GetResource("Pages_PullRequestSearchPage_UpdatedAt"),
                        Data = new DetailsLink() { Text = $"{_timeSpanHelper.DateTimeOffsetToDisplayString(item.Creator?.UpdatedAt, null)}" },
                    },
                    new DetailsElement()
                    {
                        Key = _resources.GetResource("Pages_PullRequestSearchPage_TargetBranch"),
                        Data = new DetailsLink() { Text = $"{item.TargetBranch}" },
                    },
                    new DetailsElement()
                    {
                        Key = _resources.GetResource("Pages_PullRequestSearchPage_PolicyStatus"),
                        Data = new DetailsLink() { Text = $"{item.PolicyStatus}" },
                    },
                    new DetailsElement()
                    {
                        Key = _resources.GetResource("Pages_PullRequestSearchPage_PolicyStatusReason"),
                        Data = new DetailsLink() { Text = !string.IsNullOrEmpty(item.PolicyStatusReason) ? $"{item.PolicyStatusReason}" : _resources.GetResource("Pages_PullRequestSearchPage_PolicyStatusReasonNone") },
                    },
                    new DetailsElement()
                    {
                        Key = _resources.GetResource("Pages_PullRequestSearchPage_InternalId"),
                        Data = new DetailsLink() { Text = $"{item.InternalId}" },
                    },
                    new DetailsElement()
                    {
                        Key = _resources.GetResource("Pages_PullRequestSearchPage_CreationDate"),
                        Data = new DetailsLink() { Text = $"{new DateTime(item.CreationDate)}" },
                    },
                },
            },
        };
    }
}
