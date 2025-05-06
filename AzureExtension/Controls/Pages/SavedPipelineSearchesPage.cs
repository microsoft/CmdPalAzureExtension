// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls.ListItems;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Pages;

public class SavedPipelineSearchesPage : ListPage
{
    // a page that lists IPipelineSearches
    public override string Title => "Saved Pipeline Searches";

    public override string Name => "Saved Pipeline Searches"; // Title is for the Page, Name is for the command

    public override IconInfo Icon => IconLoader.GetIcon("Logo");

    private readonly IResources _resources;

    private readonly AddPipelineListItem _addPipelineListItem;

    public SavedPipelineSearchesPage(IResources resources, AddPipelineListItem addPipelineListItem)
    {
        _resources = resources;
        _addPipelineListItem = addPipelineListItem;
    }

    public override IListItem[] GetItems()
    {
        return new[]
        {
            new ListItem(new PipelineDefinitionPage()),
            new ListItem(new BuildPage()),
            _addPipelineListItem,
        };
    }
}
