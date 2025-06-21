// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls.Forms;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Pages;

public sealed partial class SaveQueryPage : ContentPage, IDisposable
{
    private readonly SaveQueryForm _saveQueryForm;
    private readonly IResources _resources;
    private readonly SavedAzureSearchesMediator _mediator;

    public SaveQueryPage(SaveQueryForm saveQueryForm, IResources resources, SavedAzureSearchesMediator mediator)
    {
        _saveQueryForm = saveQueryForm;
        _resources = resources;
        Icon = _saveQueryForm.IsEditing ? IconLoader.GetIcon("Edit") : IconLoader.GetIcon("Add");
        Title = _saveQueryForm.IsEditing ? _resources.GetResource("Pages_EditQuery") : _resources.GetResource("Pages_SaveQuery_Title");
        Name = Title; // Name is for commands, title is for the page
        _mediator = mediator;
        _mediator.LoadingStateChanged += OnLoadingStateChanged;
    }

    public override IContent[] GetContent()
    {
        return [_saveQueryForm];
    }

    private void OnLoadingStateChanged(object? sender, bool isLoading)
    {
        IsLoading = isLoading;
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
