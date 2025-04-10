// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Nodes;
using AzureExtension.Client;
using AzureExtension.DeveloperId;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Serilog;

namespace AzureExtension.Controls.Forms;

public sealed partial class SaveQueryForm : FormContent, IAzureForm
{
    private readonly Query _savedQuery;

    private readonly IResources _resources;

    private readonly SavedQueriesMediator _savedQueriesMediator;

    private readonly IAccountProvider _accountProvider;

    private readonly AzureClientHelpers _azureClientHelpers;

    public event EventHandler<bool>? LoadingStateChanged;

    private readonly string isTopLevelChecked = "false";

    public event EventHandler<FormSubmitEventArgs>? FormSubmitted;

    public Dictionary<string, string> TemplateSubstitutions => new()
    {
        { "{{SaveQueryFormTitle}}", _resources.GetResource(string.IsNullOrEmpty(_savedQuery.Name) ? "Forms_Save_Query" : "Forms_Edit_Query") },
        { "{{SavedQueryString}}", _savedQuery.AzureUri.ToString() },
        { "{{SavedQueryName}}", _savedQuery.Name },
        { "{{IsTopLevel}}", isTopLevelChecked },
        { "{{EnteredQueryErrorMessage}}", _resources.GetResource("Forms_SaveQueryTemplateEnteredQueryError") },
        { "{{EnteredQueryLabel}}", _resources.GetResource("Forms_SaveQueryTemplateEnteredQueryLabel") },
        { "{{NameLabel}}", _resources.GetResource("Forms_SaveQueryTemplateNameLabel") },
        { "{{NameErrorMessage}}", _resources.GetResource("Forms_SaveQueryTemplateNameError") },
        { "{{IsTopLevelTitle}}", _resources.GetResource("Forms_SaveQueryTemplateIsTopLevelTitle") },
        { "{{SaveQueryActionTitle}}", _resources.GetResource("Forms_SaveQueryTemplateSaveQueryActionTitle") },
    };

    // for saving a new query
    public SaveQueryForm(IResources resources, SavedQueriesMediator savedQueriesMediator, IAccountProvider accountProvider, AzureClientHelpers azureClientHelpers)
    {
        _resources = resources;
        _savedQuery = new Query();
        _savedQueriesMediator = savedQueriesMediator;
        _accountProvider = accountProvider;
        _azureClientHelpers = azureClientHelpers;
    }

    // for editing an existing query
    public SaveQueryForm(Query savedQuery, IResources resources, SavedQueriesMediator savedQueriesMediator, IAccountProvider accountProvider, AzureClientHelpers azureClientHelpers)
    {
        _resources = resources;
        _savedQuery = savedQuery;
        _savedQueriesMediator = savedQueriesMediator;
        _accountProvider = accountProvider;
        _azureClientHelpers = azureClientHelpers;
    }

    public override string TemplateJson => TemplateHelper.LoadTemplateJsonFromTemplateName("SaveQuery", TemplateSubstitutions);

    public override ICommandResult SubmitForm(string inputs, string data)
    {
        LoadingStateChanged?.Invoke(this, true);
        Task.Run(() =>
        {
            var query = GetQuery(inputs);
            ExtensionHost.LogMessage(new LogMessage() { Message = $"Query: {query}" });
        });

        return CommandResult.KeepOpen();
    }

    public Query GetQuery(string payload)
    {
        try
        {
            var payloadJson = JsonNode.Parse(payload) ?? throw new InvalidOperationException("No query found");

            var query = CreateQueryFromJson(payloadJson);

            // if editing the query, delete the old one
            // it is safe to do as the new one is already validated
            if (_savedQuery.AzureUri.ToString() != string.Empty)
            {
                Log.Information($"Removing outdated query {_savedQuery.Name}, {_savedQuery.AzureUri.ToString()}");

                _savedQueriesMediator.RemoveQuery(_savedQuery);
            }

            LoadingStateChanged?.Invoke(this, false);
            _savedQueriesMediator.AddQuery(query);
            FormSubmitted?.Invoke(this, new FormSubmitEventArgs(true, null));
            return query;
        }
        catch (Exception ex)
        {
            LoadingStateChanged?.Invoke(this, false);
            _savedQueriesMediator.AddQuery(ex);
            FormSubmitted?.Invoke(this, new FormSubmitEventArgs(false, ex));
        }

        return new Query();
    }

    public Query CreateQueryFromJson(JsonNode? jsonNode)
    {
        var queryUrl = jsonNode?["EnteredQuery"]?.ToString() ?? string.Empty;
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

        return new Query(queryInfo.AzureUri, name, queryInfo.Description);
    }
}
