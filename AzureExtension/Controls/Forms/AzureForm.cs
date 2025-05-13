// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Text.Json.Nodes;
using AzureExtension.Account;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Forms;

#pragma warning disable SA1649 // File name should match first type name
public abstract class AzureForm<TSearch> : FormContent, IAzureForm
    where TSearch : IAzureSearch
{
    private readonly ISavedSearchesUpdater<TSearch> _savedSearchesUpdater;
    private readonly SavedAzureSearchesMediator _mediator;
    private readonly IAccountProvider _accountProvider;

    protected TSearch? SavedSearch { get; set; }

    public event EventHandler<bool>? LoadingStateChanged;

    public event EventHandler<FormSubmitEventArgs>? FormSubmitted;

    protected string IsTopLevelChecked => GetIsTopLevel().ToString().ToLower(CultureInfo.InvariantCulture);

    public abstract Dictionary<string, string> TemplateSubstitutions { get; }

    public string TemplateKey { get; set; } = string.Empty;

    public override string TemplateJson => TemplateHelper.LoadTemplateJsonFromTemplateName(TemplateKey, TemplateSubstitutions);

    public AzureForm(
        TSearch? search,
        ISavedSearchesUpdater<TSearch> savedSearchesUpdater,
        SavedAzureSearchesMediator mediator,
        IAccountProvider accountProvider)
    {
        SavedSearch = search;
        _savedSearchesUpdater = savedSearchesUpdater;
        _mediator = mediator;
        _accountProvider = accountProvider;
    }

    public override ICommandResult SubmitForm(string inputs, string data)
    {
        Task.Run(async () =>
        {
            await HandleInputs(inputs);
        });

        return CommandResult.KeepOpen();
    }

    private async Task HandleInputs(string inputs)
    {
        LoadingStateChanged?.Invoke(this, true);

        try
        {
            var payloadJson = JsonNode.Parse(inputs) ?? throw new InvalidOperationException("No search found");

            var search = CreateSearchFromJson(payloadJson);

            await _savedSearchesUpdater.Validate(search, _accountProvider.GetDefaultAccount());

            // if editing the search, delete the old one
            // it is safe to do as the new one is already validated
            if (!string.IsNullOrEmpty(SavedSearch?.Url))
            {
                // Log.Information($"Removing outdated search {_savedDefinitionSearch!.InternalId}");
                _savedSearchesUpdater.RemoveSavedSearch(SavedSearch!);
            }

            LoadingStateChanged?.Invoke(this, false);
            _savedSearchesUpdater.AddOrUpdateSearch(search, search.IsTopLevel);
            _mediator.AddPipelineSearch(search);

            if (SavedSearch != null)
            {
                SavedSearch = search;
            }

            FormSubmitted?.Invoke(this, new FormSubmitEventArgs(true, null));
        }
        catch (Exception ex)
        {
            LoadingStateChanged?.Invoke(this, false);
            _mediator.AddPipelineSearch(ex);
            FormSubmitted?.Invoke(this, new FormSubmitEventArgs(false, ex));
        }
    }

    protected abstract TSearch CreateSearchFromJson(JsonNode jsonNode);

    public bool GetIsTopLevel()
    {
        if (SavedSearch == null || string.IsNullOrEmpty(SavedSearch.Url))
        {
            return false;
        }

        return _savedSearchesUpdater.IsTopLevel(SavedSearch!);
    }
}
