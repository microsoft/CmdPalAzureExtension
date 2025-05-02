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

    private readonly TimeSpanHelper _timeSpanHelper;

    public WorkItemsSearchPage(IQuery query, IResources resources, IDataProvider dataProvider, TimeSpanHelper timeSpanHelper)
        : base(query, dataProvider)
    {
        _query = query;
        _resources = resources;
        _dataProvider = dataProvider;
        _timeSpanHelper = timeSpanHelper;
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
            MoreCommands = new CommandContextItem[]
            {
                new(new CopyCommand(item.InternalId.ToStringInvariant(), "Copy work item ID")),
                new(new CopyCommand(item.HtmlUrl, "Copy work item URL")),
            },
            Details = new Details()
            {
                Title = item.SystemTitle,
                Metadata = new[]
                {
                    new DetailsElement()
                    {
                        Key = "Reason:",
                        Data = new DetailsLink() { Text = $"{item.SystemReason}" },
                    },
                    new DetailsElement()
                    {
                        Key = "Assigned to:",
                        Data = new DetailsLink() { Text = $"{item.SystemAssignedTo?.Name ?? "Unassigned"}" },
                    },
                    new DetailsElement()
                    {
                        Key = "Last changed:",
                        Data = new DetailsLink() { Text = $"{_timeSpanHelper.DateTimeOffsetToDisplayString(new DateTime(item.SystemChangedDate), null)}" },
                    },
                    new DetailsElement()
                    {
                        Key = "Created:",
                        Data = new DetailsLink() { Text = $"{new DateTime(item.SystemCreatedDate)}" },
                    },
                    new DetailsElement()
                    {
                        Data = new DetailsTags()
                        {
                            Tags = new[]
                            {
                                GetStatusTag(item),
                            },
                        },
                    },
                },
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
}
