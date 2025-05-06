// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls.ListItems;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Pages;

public class SavedPipelinesPage : ListPage
{
    public override string Title => "Saved Pipelines";

    public override string Name => "Saved Pipelines"; // Title is for the Page, Name is for the command

    public override IconInfo Icon => IconLoader.GetIcon("Logo");

    private readonly IResources _resources;

    public SavedPipelinesPage(IResources resources)
    {
        _resources = resources;
    }

    public override IListItem[] GetItems()
    {
        return new[]
        {
            new ListItem(new PipelineDefinitionPage()),
            new ListItem(new BuildPage()),
            new AddPipelineSearchListItem(new SavePipelinePage(_resources)),
        };
    }
}
