// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls.Forms;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Pages;

public sealed partial class SavePipelineSearchPage : ContentPage, IDisposable
{
    private readonly SavePipelineSearchForm _savePipelineSearchForm;
    private readonly IResources _resources;
    private readonly SavedAzureSearchesMediator _mediator;

    public SavePipelineSearchPage(SavePipelineSearchForm savePipelineSearchForm, IResources resources, SavedAzureSearchesMediator mediator)
    {
        _savePipelineSearchForm = savePipelineSearchForm;
        _resources = resources;
        _mediator = mediator;
        Icon = _savePipelineSearchForm.IsEditing ? IconLoader.GetIcon("Edit") : IconLoader.GetIcon("Add");
        Title = _savePipelineSearchForm.IsEditing
            ? _resources.GetResource("Pages_EditPipelineSearch")
            : _resources.GetResource("Pages_SavePipelineSearch_Title");
        Name = Title; // Name is for commands, title is for the page
        _mediator.LoadingStateChanged += OnLoadingStateChanged;
    }

    public override IContent[] GetContent()
    {
        return [_savePipelineSearchForm];
    }

    private void OnLoadingStateChanged(object? sender, SearchSetLoadingStateArgs args)
    {
        IsLoading = args.IsLoading && args.SearchType == SearchUpdatedType.Pipeline;
    }

    // Disposing area
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
