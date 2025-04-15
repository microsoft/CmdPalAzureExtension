// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.DataModel;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls;

public class WorkItem
{
    public int Id { get; set; }

    public string SystemTitle { get; set; } = string.Empty;

    public string HtmlUrl { get; set; } = string.Empty;

    public string SystemState { get; set; } = string.Empty;

    public string SystemReason { get; set; } = string.Empty;

    public Identity SystemAssignedTo { get; set; }

    public long SystemCreatedDate { get; set; }

    public Identity SystemCreatedBy { get; set; }

    public long SystemChangedDate { get; set; }

    public Identity SystemChangedBy { get; set; }

    public WorkItemType SystemWorkItemType { get; set; }

    public IconInfo Icon { get; set; }

    public IconInfo StatusIcon { get; set; }

    public WorkItem()
    {
        SystemAssignedTo = new Identity();
        SystemCreatedBy = new Identity();
        SystemChangedBy = new Identity();
        SystemWorkItemType = new WorkItemType();
        Icon = new IconInfo(string.Empty);
        StatusIcon = new IconInfo(string.Empty);
    }

    public void AddSystemId(int? id)
    {
        if (id != null)
        {
            Id = (int)id;
        }
    }

    public void AddSystemTitle(string title)
    {
        if (!string.IsNullOrEmpty(title))
        {
            SystemTitle = title;
        }
    }

    public void AddHtmlUrl(string htmlUrl)
    {
        if (!string.IsNullOrEmpty(htmlUrl))
        {
            HtmlUrl = htmlUrl;
        }
    }
}
