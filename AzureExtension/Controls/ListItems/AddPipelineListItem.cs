// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls.Pages;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.ListItems
{
    public partial class AddPipelineListItem : ListItem
    {
        public AddPipelineListItem(SavePipelinePage page)
        : base(page)
        {
            Title = "Add Pipeline";
            Icon = IconLoader.GetIcon("Add");
        }

        public AddPipelineListItem(SavePipelineSearchPage page)
        : base(page)
        {
            Title = "Add Pipelines by Project";
            Icon = IconLoader.GetIcon("Add");
        }
    }
}
