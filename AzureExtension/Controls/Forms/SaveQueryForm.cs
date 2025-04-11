// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    private readonly IQuery _savedSearch;
    private readonly IResources _resources;
    private readonly SavedQueriesMediator _savedSearchesMediator;
    private readonly IAccountProvider _accountProvider;
    private readonly AzureClientHelpers _azureClientHelpers;
    private readonly IQueryRepository _queryRepository;

    public event EventHandler<bool>? LoadingStateChanged;

    private readonly string isTopLevelChecked = "false";

    public event EventHandler<FormSubmitEventArgs>? FormSubmitted;

    public Dictionary<string, string> TemplateSubstitutions => new()
    {
        { "{{SaveSearchFormTitle}}", _resources.GetResource(string.IsNullOrEmpty(_savedSearch.Name) ? "Forms_Save_Search" : "Forms_Edit_Search") },
        { "{{SavedSearchString}}", _savedSearch.Url },
        { "{{SavedSearchName}}", _savedSearch.Name },
        { "{{IsTopLevel}}", isTopLevelChecked },
        { "{{EnteredSearchErrorMessage}}", _resources.GetResource("Forms_SaveSearchTemplateEnteredSearchError") },
        { "{{EnteredSearchLabel}}", _resources.GetResource("Forms_SaveSearchTemplateEnteredSearchLabel") },
        { "{{NameLabel}}", _resources.GetResource("Forms_SaveSearchTemplateNameLabel") },
        { "{{NameErrorMessage}}", _resources.GetResource("Forms_SaveSearchTemplateNameError") },
        { "{{IsTopLevelTitle}}", _resources.GetResource("Forms_SaveSearchTemplateIsTopLevelTitle") },
        { "{{SaveSearchActionTitle}}", _resources.GetResource("Forms_SaveSearchTemplateSaveSearchActionTitle") },
    };

    // for saving a new query
    public SaveQueryForm(IResources resources, SavedQueriesMediator savedSearchesMediator, IAccountProvider accountProvider, AzureClientHelpers azureClientHelpers, IQueryRepository queryRepository)
    {
        _resources = resources;
        _savedSearch = new Query();
        _savedSearchesMediator = savedSearchesMediator;
        _accountProvider = accountProvider;
        _azureClientHelpers = azureClientHelpers;
        _queryRepository = queryRepository;
    }

    // for editing an existing query
    public SaveQueryForm(IQuery savedSearch, IResources resources, SavedQueriesMediator savedSearchesMediator, IAccountProvider accountProvider, AzureClientHelpers azureClientHelpers, IQueryRepository queryRepository)
    {
        _resources = resources;
        _savedSearch = savedSearch;
        _savedSearchesMediator = savedSearchesMediator;
        _accountProvider = accountProvider;
        _azureClientHelpers = azureClientHelpers;
        _queryRepository = queryRepository;
    }

    public override string TemplateJson => TemplateHelper.LoadTemplateJsonFromTemplateName("SaveSearch", TemplateSubstitutions);

    public override ICommandResult SubmitForm(string inputs, string data)
    {
        LoadingStateChanged?.Invoke(this, true);
        Task.Run(() =>
        {
            var search = GetSearch(inputs);
            ExtensionHost.LogMessage(new LogMessage() { Message = $"Search: {search}" });
        });

        return CommandResult.KeepOpen();
    }

    public Query GetSearch(string payload)
    {
        try
        {
            var payloadJson = JsonNode.Parse(payload) ?? throw new InvalidOperationException("No search found");

            var query = CreateQueryFromJson(payloadJson);

            // if editing the search, delete the old one
            // it is safe to do as the new one is already validated
            if (_savedSearch.Url != string.Empty)
            {
                Log.Information($"Removing outdated search {_savedSearch.Name}, {_savedSearch.Url}");

                _savedSearchesMediator.RemoveSearch(_savedSearch);
            }

            LoadingStateChanged?.Invoke(this, false);
            _queryRepository.AddSavedQueryAsync(query).Wait();
            _savedSearchesMediator.AddSearch(query);
            FormSubmitted?.Invoke(this, new FormSubmitEventArgs(true, null));
            return query;
        }
        catch (Exception ex)
        {
            LoadingStateChanged?.Invoke(this, false);
            _savedSearchesMediator.AddSearch(ex);
            FormSubmitted?.Invoke(this, new FormSubmitEventArgs(false, ex));
        }

        return new Query();
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

        return new Query(queryInfo.AzureUri, name, queryInfo.Description);
    }
}
