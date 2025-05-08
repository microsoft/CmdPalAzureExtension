// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Text.Json.Nodes;
using AzureExtension.Account;
using AzureExtension.Client;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Serilog;

namespace AzureExtension.Controls.Forms;

public sealed partial class SaveQueryForm : FormContent, IAzureForm
{
    private readonly IQuery _savedQuery;
    private readonly IResources _resources;
    private readonly SavedAzureSearchesMediator _savedQueriesMediator;
    private readonly IAccountProvider _accountProvider;
    private readonly AzureClientHelpers _azureClientHelpers;
    private readonly ISavedSearchesUpdater<IQuery> _queryRepository;

    public event EventHandler<bool>? LoadingStateChanged;

    private string IsTopLevelChecked => GetIsTopLevel().ToString().ToLower(CultureInfo.InvariantCulture);

    public event EventHandler<FormSubmitEventArgs>? FormSubmitted;

    public Dictionary<string, string> TemplateSubstitutions => new()
    {
        { "{{SaveQueryFormTitle}}", _resources.GetResource(string.IsNullOrEmpty(_savedQuery.Name) ? "Forms_Save_Query" : "Forms_Edit_Query") },
        { "{{SavedQueryString}}", _savedQuery.Url },
        { "{{SavedQueryName}}", _savedQuery.Name },
        { "{{EnteredQueryErrorMessage}}", _resources.GetResource("Forms_SaveQuery_TemplateEnteredQueryError") },
        { "{{EnteredQueryLabel}}", _resources.GetResource("Forms_SaveQuery_TemplateEnteredQueryLabel") },
        { "{{NameLabel}}", _resources.GetResource("Forms_SaveQuery_TemplateNameLabel") },
        { "{{NameErrorMessage}}", _resources.GetResource("Forms_SaveQuery_TemplateNameError") },
        { "{{IsTopLevelTitle}}", _resources.GetResource("Forms_SaveQueryTemplate_IsTopLevelTitle") },
        { "{{IsTopLevel}}", IsTopLevelChecked },
        { "{{SaveQueryActionTitle}}", _resources.GetResource("Forms_SaveQuery_TemplateSaveQueryActionTitle") },
    };

    // for saving a new query
    public SaveQueryForm(
        IResources resources,
        SavedAzureSearchesMediator savedQueriesMediator,
        IAccountProvider accountProvider,
        AzureClientHelpers azureClientHelpers,
        ISavedSearchesUpdater<IQuery> queryRepository)
    {
        _resources = resources;
        _savedQuery = new Query();
        _savedQueriesMediator = savedQueriesMediator;
        _accountProvider = accountProvider;
        _azureClientHelpers = azureClientHelpers;
        _queryRepository = queryRepository;
    }

    // for editing an existing query
    public SaveQueryForm(
        IQuery savedQuery,
        IResources resources,
        SavedAzureSearchesMediator savedQueriesMediator,
        IAccountProvider accountProvider,
        AzureClientHelpers azureClientHelpers,
        ISavedSearchesUpdater<IQuery> queryRepository)
    {
        _resources = resources;
        _savedQuery = savedQuery;
        _savedQueriesMediator = savedQueriesMediator;
        _accountProvider = accountProvider;
        _azureClientHelpers = azureClientHelpers;
        _queryRepository = queryRepository;
    }

    public override string TemplateJson => TemplateHelper.LoadTemplateJsonFromTemplateName("SaveQuery", TemplateSubstitutions);

    public override ICommandResult SubmitForm(string inputs, string data)
    {
        LoadingStateChanged?.Invoke(this, true);
        Task.Run(async () =>
        {
            await AddQuery(inputs);
        });

        return CommandResult.KeepOpen();
    }

    public async Task AddQuery(string payload)
    {
        try
        {
            var payloadJson = JsonNode.Parse(payload) ?? throw new InvalidOperationException("No search found");

            var query = CreateQueryFromJson(payloadJson);

            // if editing the search, delete the old one
            // it is safe to do as the new one is already validated
            if (_savedQuery.Url != string.Empty)
            {
                Log.Information($"Removing outdated search {_savedQuery.Name}, {_savedQuery.Url}");

                _queryRepository.RemoveSavedSearch(_savedQuery);
            }

            LoadingStateChanged?.Invoke(this, false);
            await _queryRepository.AddOrUpdateSearch(query, query.IsTopLevel, _accountProvider.GetDefaultAccount());
            _savedQueriesMediator.AddQuery(query);
            FormSubmitted?.Invoke(this, new FormSubmitEventArgs(true, null));
        }
        catch (Exception ex)
        {
            LoadingStateChanged?.Invoke(this, false);
            _savedQueriesMediator.AddQuery(ex);
            FormSubmitted?.Invoke(this, new FormSubmitEventArgs(false, ex));
        }
    }

    public Query CreateQueryFromJson(JsonNode? jsonNode)
    {
        var queryUrl = jsonNode?["EnteredQuery"]?.ToString() ?? string.Empty;
        var name = jsonNode?["Name"]?.ToString() ?? string.Empty;
        var isTopLevel = jsonNode?["IsTopLevel"]?.ToString() == "true";

        var account = _accountProvider.GetDefaultAccount();
        var queryInfo = _azureClientHelpers.GetInfo(queryUrl, account, InfoType.Query).Result;

        if (queryInfo.Result != ResultType.Success)
        {
            var error = queryInfo.Error;
            throw new InvalidOperationException($"Failed to get query info {queryInfo.Error}: {queryInfo.ErrorMessage}");
        }

        if (string.IsNullOrEmpty(name))
        {
            name = queryInfo.Name;
        }

        var uri = queryInfo.AzureUri;
        return new Query(uri, name, queryInfo.Description, isTopLevel);
    }

    public bool GetIsTopLevel()
    {
        return _queryRepository.IsTopLevel(_savedQuery);
    }
}
