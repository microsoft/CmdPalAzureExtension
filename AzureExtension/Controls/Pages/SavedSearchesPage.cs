// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Pages;

public abstract class SavedSearchesPage : ListPage
{
    protected abstract SearchUpdatedType SearchUpdatedType { get; }

    protected abstract string ExceptionMessage { get; }

    private readonly SavedAzureSearchesMediator _mediator;

    public SavedSearchesPage(SavedAzureSearchesMediator mediator)
    {
        _mediator = mediator;
        _mediator.SearchUpdated += OnSearchUpdated;
    }

    private void OnSearchUpdated(object? sender, SearchUpdatedEventArgs args)
    {
        IsLoading = false;

        if (args.SearchType != SearchUpdatedType)
        {
            return;
        }

        if (args.Exception != null)
        {
            var toast = new ToastStatusMessage(new StatusMessage()
            {
                Message = ExceptionMessage,
                State = MessageState.Error,
            });

            toast.Show();
            return;
        }

        switch (args.EventType)
        {
            case SearchUpdatedEventType.SearchAdded:
                RaiseItemsChanged();
                break;
            case SearchUpdatedEventType.SearchRemoved:
                RaiseItemsChanged();
                break;
            case SearchUpdatedEventType.SearchRemoving:
                IsLoading = true;
                break;
            default:
                break;
        }
    }
}
