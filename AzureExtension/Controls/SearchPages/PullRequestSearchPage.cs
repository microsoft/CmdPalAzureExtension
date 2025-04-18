// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls.Commands;
using AzureExtension.DataManager;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.SearchPages;

public sealed partial class PullRequestSearchPage : SearchPage<IPullRequest>
{
    private readonly IPullRequestSearch _search;

    private readonly IResources _resources;

    private readonly IDataProvider _dataProvider;

    public PullRequestSearchPage(IPullRequestSearch search, IResources resources, IDataProvider dataProvider)
        : base(search)
    {
        _search = search;
        _resources = resources;
        _dataProvider = dataProvider;
        Icon = new IconInfo(AzureIcon.IconDictionary["logo"]);
        Name = search.Name;
    }

    protected override ListItem GetListItem(IPullRequest item)
    {
        var title = item.Title;
        var url = item.HtmlUrl;

        return new ListItem(new LinkCommand(url, _resources))
        {
            Title = title,
            Icon = new IconInfo(AzureIcon.IconDictionary["logo"]),
        };
    }

    protected override Task<IEnumerable<IPullRequest>> LoadContentData()
    {
        return _dataProvider.GetPullRequests(_search);
    }
}
