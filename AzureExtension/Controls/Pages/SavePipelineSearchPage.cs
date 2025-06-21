// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls.Forms;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Pages;

public class SavePipelineSearchPage : ContentPage
{
    private readonly IResources _resources;
    private readonly SavePipelineSearchForm _savePipelineSearchForm;
    private readonly SavedAzureSearchesMediator _mediator;

    public SavePipelineSearchPage(IResources resources, SavePipelineSearchForm savePipelineSearchForm, SavedAzureSearchesMediator mediator)
    {
        _resources = resources;
        _savePipelineSearchForm = savePipelineSearchForm;
        _mediator = mediator;
        Title = _resources.GetResource("Pages_SavePipelineSearch_Title");
        Name = Title; // Name is for commands, title is for the page
        Icon = IconLoader.GetIcon("Add");
        _mediator.LoadingStateChanged += OnLoadingStateChanged;
    }

    public override IContent[] GetContent()
    {
        return [_savePipelineSearchForm];
    }

    private void OnLoadingStateChanged(object? sender, bool isLoading)
    {
        IsLoading = isLoading;
    }
}
