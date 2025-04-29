// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls.Commands;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Pages;

public partial class WorkItemsSearchPage : SearchPage<IWorkItem>
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
        ShowDetails = true;
    }

    protected override ListItem GetListItem(IWorkItem item)
    {
        var title = item.SystemTitle;
        var url = item.HtmlUrl;

        return new ListItem(new LinkCommand(url, _resources))
        {
            Title = title,
            Icon = IconLoader.GetIcon(item.WorkItemTypeName),
            Tags = new[] { GetStatusTag(item) },

            Details = new Details()
            {
                HeroImage = new IconInfo(item.SystemCreatedBy?.Avatar),
                Title = item.SystemTitle,
                Body = GetMarkdownText(item),
            },
        };
    }

    protected async override Task<IEnumerable<IWorkItem>> LoadContentData()
    {
        return await _dataProvider.GetWorkItems(_query);
    }

    protected ITag GetStatusTag(IWorkItem item)
    {
        var color = item.SystemState switch
        {
            "Active" => "StatusRed",
            "Committed" => "StatusBlue",
            "Started" => "StatusBlue",
            "Completed" => "StatusGreen",
            "Closed" => "StatusGreen",
            "Resolved" => "StatusBlue",
            "Proposed" => "StatusGray",
            "Cut" => "StatusGray",
            _ => "StatusGray",
        };

        return new Tag()
        {
            Text = item.SystemState,
            Icon = IconLoader.GetIcon(color),
        };
    }

    private string GetMarkdownText(IWorkItem item)
    {
        return $@"
System work item type: {item.WorkItemTypeName}

Internal id: {item.Id}

State: {item.SystemState}

Reason: {item.SystemReason}

Assigned to: {item.SystemAssignedTo?.Name ?? "Unassigned"}

Changed date: {item.SystemChangedDate}
        ";
    }
}
