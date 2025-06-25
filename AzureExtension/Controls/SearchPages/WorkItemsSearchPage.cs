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
    private readonly IResources _resources;
    private readonly TimeSpanHelper _timeSpanHelper;

    public WorkItemsSearchPage(
        IQuerySearch query,
        IResources resources,
        ILiveContentDataProvider<IWorkItem> contentDataProvider,
        TimeSpanHelper timeSpanHelper)
        : base(query, contentDataProvider, resources)
    {
        _resources = resources;
        _timeSpanHelper = timeSpanHelper;
        Icon = IconLoader.GetIcon("Query");
        Title = query.Name;
        Name = Title; // Name is for the command, title is for the page
        ShowDetails = true;
    }

    protected override ListItem GetListItem(IWorkItem item)
    {
        var title = item.SystemTitle;
        var url = item.HtmlUrl;

        return new ListItem(new LinkCommand(url, _resources, null))
        {
            Title = title,
            Icon = IconLoader.GetIcon(item.WorkItemTypeName),
            Tags = new[] { GetStatusTag(item) },
            MoreCommands = new CommandContextItem[]
            {
                new(new CopyCommand(item.InternalId.ToStringInvariant(), _resources.GetResource("Pages_WorkItemsSearchPage_CopyWorkItemId"), _resources)),
                new(new CopyCommand(item.HtmlUrl, _resources.GetResource("Pages_WorkItemsSearchPage_CopyURLCommand"), _resources)),
            },
            Details = new Details()
            {
                Title = item.SystemTitle,
                Metadata = new[]
                {
                    new DetailsElement()
                    {
                        Key = _resources.GetResource("Pages_WorkItemsSearchPage_Reason"),
                        Data = new DetailsLink() { Text = $"{item.SystemReason}" },
                    },
                    new DetailsElement()
                    {
                        Key = _resources.GetResource("Pages_WorkItemsSearchPage_AssignedTo"),
                        Data = new DetailsLink() { Text = $"{item.SystemAssignedTo?.Name ?? _resources.GetResource("Pages_WorkItemsSearchPage_Unassigned")}" },
                    },
                    new DetailsElement()
                    {
                        Key = _resources.GetResource("Pages_WorkItemsSearchPage_LastChanged"),
                        Data = new DetailsLink() { Text = $"{_timeSpanHelper.DateTimeOffsetToDisplayString(new DateTime(item.SystemChangedDate), null)}" },
                    },
                    new DetailsElement()
                    {
                        Key = _resources.GetResource("Pages_WorkItemsSearchPage_CreatedDate"),
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
