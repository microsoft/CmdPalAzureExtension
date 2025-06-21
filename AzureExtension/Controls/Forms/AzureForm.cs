// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Text.Json.Nodes;
using AzureExtension.Account;
using AzureExtension.Controls.Commands;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Forms;

#pragma warning disable SA1649 // File name should match first type name
public abstract class AzureForm<TSearch> : FormContent, IAzureForm
    where TSearch : IAzureSearch
{
    private readonly ISavedSearchesUpdater<TSearch> _savedSearchesUpdater;
    private readonly SaveSearchCommand<TSearch> _saveSearchCommand;

    protected TSearch? SavedSearch { get; set; }

    protected string IsTopLevelChecked => GetIsTopLevel().ToString().ToLower(CultureInfo.InvariantCulture);

    public abstract Dictionary<string, string> TemplateSubstitutions { get; }

    public string TemplateKey { get; set; } = string.Empty;

    public override string TemplateJson => TemplateHelper.LoadTemplateJsonFromTemplateName(TemplateKey, TemplateSubstitutions);

    public AzureForm(
        TSearch? search,
        ISavedSearchesUpdater<TSearch> savedSearchesUpdater,
        SavedAzureSearchesMediator mediator,
        IAccountProvider accountProvider,
        SaveSearchCommand<TSearch> saveSearchCommand)
    {
        SavedSearch = search;
        _savedSearchesUpdater = savedSearchesUpdater;
        _saveSearchCommand = saveSearchCommand;
    }

    public override ICommandResult SubmitForm(string inputs, string data)
    {
        var payloadJson = JsonNode.Parse(inputs) ?? throw new InvalidOperationException("No search found");
        _saveSearchCommand.SetSearchToSave(CreateSearchFromJson(payloadJson));

        return _saveSearchCommand.Invoke();
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
