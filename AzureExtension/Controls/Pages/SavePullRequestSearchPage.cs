// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls.Forms;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Pages;

public class SavePullRequestSearchPage : ContentPage
{
    private readonly SavePullRequestSearchForm _savePullRequestSearchForm;
    private readonly IResources _resources;
    private readonly SavedAzureSearchesMediator _mediator;

    public SavePullRequestSearchPage(SavePullRequestSearchForm savePullRequestSearchForm, IResources resources, SavedAzureSearchesMediator mediator)
    {
        _resources = resources;
        Title = _resources.GetResource("Pages_SavePullRequestSearch_Title");
        Icon = IconLoader.GetIcon("Add");
        _savePullRequestSearchForm = savePullRequestSearchForm;
        Name = Title; // Name is for commands, title is for the page
        _mediator = mediator;
        _mediator.LoadingStateChanged += OnLoadingStateChanged;
    }

    public override IContent[] GetContent()
    {
        return [_savePullRequestSearchForm];
    }

    private void OnLoadingStateChanged(object? sender, bool isLoading)
    {
        IsLoading = isLoading;
    }
}
