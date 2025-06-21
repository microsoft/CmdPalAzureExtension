// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls.Forms;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Pages;

public sealed partial class SavePullRequestSearchPage : ContentPage, IDisposable
{
    private readonly SavePullRequestSearchForm _savePullRequestSearchForm;
    private readonly IResources _resources;
    private readonly SavedAzureSearchesMediator _mediator;

    public SavePullRequestSearchPage(SavePullRequestSearchForm savePullRequestSearchForm, IResources resources, SavedAzureSearchesMediator mediator)
    {
        _savePullRequestSearchForm = savePullRequestSearchForm;
        _resources = resources;
        _mediator = mediator;

        Title = _savePullRequestSearchForm.IsEditing
            ? _resources.GetResource("Pages_EditPullRequestSearch")
            : _resources.GetResource("Pages_SavePullRequestSearch_Title");
        Icon = IconLoader.GetIcon(_savePullRequestSearchForm.IsEditing ? "Edit" : "Add");
        Name = Title; // Name is for commands, title is for the page

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

    // disposing area
    private bool _disposed;

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _mediator.LoadingStateChanged -= OnLoadingStateChanged;
            }

            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
