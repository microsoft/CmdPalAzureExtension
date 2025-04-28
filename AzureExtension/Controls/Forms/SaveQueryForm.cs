// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Text.Json.Nodes;
using AzureExtension.Account;
using AzureExtension.Client;
using AzureExtension.Helpers;
using AzureExtension.PersistentData;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Serilog;

namespace AzureExtension.Controls.Forms;

public sealed partial class SaveQueryForm : FormContent, IAzureForm
{
    private readonly IResources _resources;
    private readonly SavedAzureSearchesMediator _savedQueriesMediator;
    private readonly IAccountProvider _accountProvider;
    private readonly AzureClientHelpers _azureClientHelpers;
    private readonly IQueryRepository _queryRepository;
    private IQuery _savedQuery;

    public event EventHandler<bool>? LoadingStateChanged;

    private string IsTopLevelChecked => GetIsTopLevel().Result.ToString().ToLower(CultureInfo.InvariantCulture);

    public event EventHandler<FormSubmitEventArgs>? FormSubmitted;

    public Dictionary<string, string> TemplateSubstitutions => new()
    {
        { "{{SaveSearchFormTitle}}", _resources.GetResource(string.IsNullOrEmpty(_savedQuery.Name) ? "Forms_Save_Search" : "Forms_Edit_Search") },
        { "{{SavedSearchString}}", _savedQuery.Url },
        { "{{SavedSearchName}}", _savedQuery.Name },
        { "{{EnteredSearchErrorMessage}}", _resources.GetResource("Forms_SaveSearchTemplateEnteredSearchError") },
        { "{{EnteredSearchLabel}}", _resources.GetResource("Forms_SaveSearchTemplateEnteredSearchLabel") },
        { "{{NameLabel}}", _resources.GetResource("Forms_SaveSearchTemplateNameLabel") },
        { "{{NameErrorMessage}}", _resources.GetResource("Forms_SaveSearchTemplateNameError") },
        { "{{IsTopLevelTitle}}", _resources.GetResource("Forms_SaveSearchTemplateIsTopLevelTitle") },
        { "{{IsTopLevel}}", IsTopLevelChecked },
        { "{{SaveSearchActionTitle}}", _resources.GetResource("Forms_SaveSearchTemplateSaveSearchActionTitle") },
    };

    // for saving a new query
    public SaveQueryForm(IResources resources, SavedAzureSearchesMediator savedQueriesMediator, IAccountProvider accountProvider, AzureClientHelpers azureClientHelpers, IQueryRepository queryRepository)
    {
        _resources = resources;
        _savedQuery = new Query();
        _savedQueriesMediator = savedQueriesMediator;
        _accountProvider = accountProvider;
        _azureClientHelpers = azureClientHelpers;
        _queryRepository = queryRepository;
    }

    // for editing an existing query
    public SaveQueryForm(IQuery savedQuery, IResources resources, SavedAzureSearchesMediator savedQueriesMediator, IAccountProvider accountProvider, AzureClientHelpers azureClientHelpers, IQueryRepository queryRepository)
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

                await _queryRepository.RemoveSavedQueryAsync(_savedQuery);
            }

            LoadingStateChanged?.Invoke(this, false);
            _queryRepository.UpdateQueryTopLevelStatus(query, query.IsTopLevel, _accountProvider.GetDefaultAccount());
            _savedQuery = query;
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
        var queryUrl = jsonNode?["EnteredSearch"]?.ToString() ?? string.Empty;
        var name = jsonNode?["Name"]?.ToString() ?? string.Empty;
        var isTopLevel = jsonNode?["IsTopLevel"]?.ToString() == "true";

        var account = _accountProvider.GetDefaultAccount();
        var queryInfo = _azureClientHelpers.GetQueryInfo(queryUrl, account);

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

    public async Task<bool> GetIsTopLevel()
    {
        return await _queryRepository.IsTopLevel(_savedQuery);
    }
}
