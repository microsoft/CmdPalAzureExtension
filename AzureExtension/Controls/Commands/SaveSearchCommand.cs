// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Commands;

#pragma warning disable SA1649 // File name should match first type name
public class SaveSearchCommand<TSearch> : InvokableCommand
#pragma warning restore SA1649 // File name should match first type name
    where TSearch : IAzureSearch
{
    private readonly ISavedSearchesUpdater<TSearch> _savedSearchesUpdater;
    private readonly SavedAzureSearchesMediator _mediator;
    private readonly string _saveSuccessMessage = string.Empty;
    private readonly string _saveFailureMessage = string.Empty;
    private readonly string _editSuccessMessage = string.Empty;
    private readonly string _editFailureMessage = string.Empty;
    private TSearch? _searchToSave;
    private TSearch? _savedSearch;
    private SearchUpdatedType _searchUpdatedType = SearchUpdatedType.Unknown;

    public SaveSearchCommand(
        ISavedSearchesUpdater<TSearch> savedSearchesUpdater,
        SavedAzureSearchesMediator mediator,
        TSearch? savedSearch,
        string successMessage,
        string failureMessage,
        string editSuccessMessage,
        string editFailureMessage)
    {
        _savedSearchesUpdater = savedSearchesUpdater;
        _mediator = mediator;
        _savedSearch = savedSearch;
        _saveSuccessMessage = successMessage;
        _saveFailureMessage = failureMessage;
        _editSuccessMessage = editSuccessMessage;
        _editFailureMessage = editFailureMessage;
        _searchUpdatedType = SearchHelper.GetSearchUpdatedType<TSearch>();
    }

    public void SetSavedSearch(TSearch savedSearch)
    {
        _savedSearch = savedSearch;
    }

    public void SetSearchToSave(TSearch search)
    {
        _searchToSave = search;
    }

    public override CommandResult Invoke()
    {
        var editing = !string.IsNullOrEmpty(_savedSearch?.Url);
        _mediator.SetLoadingState(true, _searchUpdatedType);

        try
        {
            // If editing the search, delete the old one
            if (editing)
            {
                _savedSearchesUpdater.RemoveSavedSearch(_savedSearch!);
            }

            _savedSearchesUpdater.AddOrUpdateSearch(_searchToSave!, _searchToSave!.IsTopLevel);
            _mediator.AddSearch(_searchToSave!);

            if (_savedSearch != null)
            {
                _savedSearch = _searchToSave;
            }

            _mediator.SetLoadingState(false, _searchUpdatedType);
            ToastHelper.ShowSuccessToast(editing ? _editSuccessMessage : _saveSuccessMessage);

            return CommandResult.KeepOpen();
        }
        catch (Exception ex)
        {
            _mediator.AddSearch(null, ex);
            _mediator.SetLoadingState(false, _searchUpdatedType);
            var errorMessage = editing ? _editFailureMessage : _saveFailureMessage;
            ToastHelper.ShowErrorToast($"{errorMessage} {ex.Message}");
            return CommandResult.KeepOpen();
        }
    }
}
