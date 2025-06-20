// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Nodes;
using AzureExtension.Account;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Commands;

#pragma warning disable SA1649 // File name should match first type name
public class SaveSearchCommand<TSearch> : InvokableCommand
#pragma warning restore SA1649 // File name should match first type name
    where TSearch : IAzureSearch
{
    private readonly ISavedSearchesUpdater<TSearch> _savedSearchesUpdater;
    private readonly SavedAzureSearchesMediator _mediator;
    private readonly TSearch? _savedSearch;
    private TSearch? _searchToSave;

    public string Inputs { get; set; } = string.Empty;

    public SaveSearchCommand(
        ISavedSearchesUpdater<TSearch> savedSearchesUpdater,
        SavedAzureSearchesMediator mediator,
        TSearch? savedSearch)
    {
        _savedSearchesUpdater = savedSearchesUpdater;
        _mediator = mediator;
        _savedSearch = savedSearch;
    }

    public void SetSearchToSave(TSearch search)
    {
        _searchToSave = search;
    }

    public override CommandResult Invoke()
    {
        if (_searchToSave == null)
        {
            return CommandResult.KeepOpen();
        }

        try
        {
            // If editing the search, delete the old one
            if (!string.IsNullOrEmpty(_savedSearch?.Url))
            {
                _savedSearchesUpdater.RemoveSavedSearch(_savedSearch);
            }

            _savedSearchesUpdater.AddOrUpdateSearch(_searchToSave!, _searchToSave!.IsTopLevel);
            _mediator.AddSearch(_searchToSave!);

            return CommandResult.KeepOpen();
        }
        catch (Exception ex)
        {
            _mediator.AddSearch(null, ex);
            return CommandResult.KeepOpen();
        }
    }
}
