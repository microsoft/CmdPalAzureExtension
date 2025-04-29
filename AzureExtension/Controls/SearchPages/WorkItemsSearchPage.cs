// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls.Commands;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Pages;

public sealed partial class WorkItemsSearchPage : SearchPage<IWorkItem>
{
    private readonly IQuery _query;

    private readonly IResources _resources;

    private readonly IDataProvider _dataProvider;

    public WorkItemsSearchPage(IQuery query, IResources resources, IDataProvider dataProvider)
        : base(query, dataProvider)
    {
        _query = query;
        _resources = resources;
        _dataProvider = dataProvider;
        Icon = IconLoader.GetIcon("Query");
        Name = query.Name;
    }

    protected override ListItem GetListItem(IWorkItem item)
    {
        var title = item.SystemTitle;
        var url = item.HtmlUrl;

        return new ListItem(new LinkCommand(url, _resources))
        {
            Title = title,
            Icon = IconLoader.GetIcon(item.WorkItemTypeName),
            Tags = new[] { new Tag(item.SystemState) },
        };
    }

    protected async override Task<IEnumerable<IWorkItem>> LoadContentData()
    {
        return await _dataProvider.GetWorkItems(_query);
    }
}
