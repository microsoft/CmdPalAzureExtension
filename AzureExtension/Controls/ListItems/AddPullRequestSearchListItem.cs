// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls.Pages;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.ListItems;

public partial class AddPullRequestSearchListItem : ListItem
{
    public AddPullRequestSearchListItem(SavePullRequestSearchPage page, IResources resources)
    : base(page)
    {
        Title = resources.GetResource("ListItems_AddQuery");
        Icon = IconLoader.GetIcon("Add");
    }
}
