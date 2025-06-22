// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Text.Json.Nodes;
using AzureExtension.Account;
using AzureExtension.Client;
using AzureExtension.Controls.Commands;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Serilog;

namespace AzureExtension.Controls.Forms;

#pragma warning disable SA1649 // File name should match first type name
public abstract class SaveSearchForm<TSearch> : FormContent
    where TSearch : IAzureSearch
{
    private readonly ISavedSearchesUpdater<TSearch> _savedSearchesUpdater;
    private readonly SaveSearchCommand<TSearch> _saveSearchCommand;
    private readonly SavedAzureSearchesMediator _mediator;
    private readonly IResources _resources;
    private readonly IAccountProvider _accountProvider;
    private readonly AzureClientHelpers _azureClientHelpers;
    private readonly ILogger _logger;
    private SearchUpdatedType _searchUpdatedType = SearchUpdatedType.Unknown;

    protected TSearch? SavedSearch { get; set; }

    protected string IsTopLevelChecked => GetIsTopLevel().ToString().ToLower(CultureInfo.InvariantCulture);

    public abstract Dictionary<string, string> TemplateSubstitutions { get; }

    public string TemplateKey { get; set; } = string.Empty;

    public override string TemplateJson => TemplateHelper.LoadTemplateJsonFromTemplateName(TemplateKey, TemplateSubstitutions);

    public bool IsEditing => SavedSearch != null;

    public SaveSearchForm(
        TSearch? search,
        ISavedSearchesUpdater<TSearch> savedSearchesUpdater,
        SavedAzureSearchesMediator mediator,
        IAccountProvider accountProvider,
        SaveSearchCommand<TSearch> saveSearchCommand,
        IResources resources,
        AzureClientHelpers azureClientHelpers)
    {
        SavedSearch = search;
        _savedSearchesUpdater = savedSearchesUpdater;
        _saveSearchCommand = saveSearchCommand;
        _mediator = mediator;
        _resources = resources;
        _accountProvider = accountProvider;
        _azureClientHelpers = azureClientHelpers;
        _logger = Log.Logger.ForContext("SourceContext", nameof(SaveSearchForm<TSearch>));
        _searchUpdatedType = SearchHelper.GetSearchUpdatedType<TSearch>();
    }

    public override ICommandResult SubmitForm(string inputs, string data)
    {
        _mediator.SetLoadingState(true, _searchUpdatedType);
        var payloadJson = JsonNode.Parse(inputs) ?? throw new InvalidOperationException("No search found");
        _logger.Information($"SaveSearchForm: ParseFormSubmission with payload {payloadJson}");
        ParseFormSubmission(payloadJson);

        var searchInfoParameters = GetSearchInfoParameters();

        _logger.Information($"SaveSearchForm: GetSearchInfo with URL {searchInfoParameters.Url} and InfoType {searchInfoParameters.InfoType}");
        var searchInfo = GetSearchInfo(searchInfoParameters);
        if (searchInfo.Result != ResultType.Success)
        {
            _mediator.SetLoadingState(false, _searchUpdatedType);
            var errorMessage = string.Format(CultureInfo.CurrentCulture, "{0}", searchInfo.ErrorMessage);
            _logger.Information($"SaveSearchForm: Error encountered - {errorMessage}");
            ToastHelper.ShowErrorToast(errorMessage);
            return CommandResult.KeepOpen();
        }

        var search = CreateSearchFromSearchInfo(searchInfo);

        _saveSearchCommand.SetSearchToSave(search);
        if (SavedSearch != null)
        {
            _saveSearchCommand.SetSavedSearch(SavedSearch);
        }

        return _saveSearchCommand.Invoke();
    }

    protected abstract SearchInfoParameters GetSearchInfoParameters();

    protected abstract TSearch CreateSearchFromSearchInfo(InfoResult searchInfo);

    protected abstract void ParseFormSubmission(JsonNode? jsonNode);

    public InfoResult GetSearchInfo(SearchInfoParameters parameters)
    {
        var account = _accountProvider.GetDefaultAccount();

        return parameters switch
        {
            DefinitionInfoParameters defParams when defParams.DefinitionId > 0 =>
                _azureClientHelpers.GetInfo(defParams.Url, account, defParams.InfoType, defParams.DefinitionId).Result,
                _ => _azureClientHelpers.GetInfo(parameters.Url, account, parameters.InfoType).Result,
        };
    }

    public bool GetIsTopLevel()
    {
        if (SavedSearch == null || string.IsNullOrEmpty(SavedSearch.Url))
        {
            return false;
        }

        return _savedSearchesUpdater.IsTopLevel(SavedSearch!);
    }
}
