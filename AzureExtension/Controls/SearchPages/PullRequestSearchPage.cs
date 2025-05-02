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
    }

    protected override ListItem GetListItem(IPullRequest item)
    {
        var title = item.Title;
        var url = item.HtmlUrl;

        return new ListItem(new LinkCommand(url, _resources))
        {
            Title = title,
            Icon = IconLoader.GetIcon("PullRequest"),
        };
    }

    protected override Task<IEnumerable<IPullRequest>> LoadContentData()
    {
        return _dataProvider.GetPullRequests(_search);
    }
}
