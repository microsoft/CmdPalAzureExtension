// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.Xml;
using System.Text.Json.Nodes;
using AzureExtension.Client;
using AzureExtension.DeveloperId;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.Extensions.Configuration;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AzureExtension.Controls.Forms;

public sealed partial class SaveSearchForm : FormContent, IAzureForm
{
    private readonly ISearch _savedSearch;

    private readonly IResources _resources;

    private readonly SavedSearchesMediator _savedSearchesMediator;

    private readonly IDeveloperIdProvider _developerIdProvider;

    public event EventHandler<bool>? LoadingStateChanged;

    private string IsTopLevelChecked => GetIsTopLevel().ToString().ToLower(CultureInfo.InvariantCulture);

    public event EventHandler<FormSubmitEventArgs>? FormSubmitted;

    public Dictionary<string, string> TemplateSubstitutions => new()
    {
        { "{{SaveSearchFormTitle}}", _resources.GetResource(string.IsNullOrEmpty(_savedSearch.Name) ? "Forms_Save_Search" : "Forms_Edit_Search") },
        { "{{SavedSearchString}}", _savedSearch.SearchString },
        { "{{SavedSearchName}}", _savedSearch.Name },
        { "{{IsTopLevel}}", IsTopLevelChecked },
        { "{{EnteredSearchErrorMessage}}", _resources.GetResource("Forms_SaveSearchTemplateEnteredSearchError") },
        { "{{EnteredSearchLabel}}", _resources.GetResource("Forms_SaveSearchTemplateEnteredSearchLabel") },
        { "{{NameLabel}}", _resources.GetResource("Forms_SaveSearchTemplateNameLabel") },
        { "{{NameErrorMessage}}", _resources.GetResource("Forms_SaveSearchTemplateNameError") },
        { "{{IsTopLevelTitle}}", _resources.GetResource("Forms_SaveSearchTemplateIsTopLevelTitle") },
        { "{{SaveSearchActionTitle}}", _resources.GetResource("Forms_SaveSearchTemplateSaveSearchActionTitle") },
    };

    // for saving a new query
    public SaveSearchForm(IResources resources, SavedSearchesMediator savedSearchesMediator, IDeveloperIdProvider developerIdProvider)
    {
        _resources = resources;
        _savedSearch = new SearchCandidate();
        _savedSearchesMediator = savedSearchesMediator;
        _developerIdProvider = developerIdProvider;
    }

    // for editing an existing query
    public SaveSearchForm(ISearch savedSearch, IResources resources, SavedSearchesMediator savedSearchesMediator, IDeveloperIdProvider developerIdProvider)
    {
        _resources = resources;
        _savedSearch = savedSearch;
        _savedSearchesMediator = savedSearchesMediator;
        _developerIdProvider = developerIdProvider;
    }

    public override string TemplateJson => TemplateHelper.LoadTemplateJsonFromTemplateName("SaveSearch", TemplateSubstitutions);

    public override ICommandResult SubmitForm(string? inputs, string data)
    {
        LoadingStateChanged?.Invoke(this, true);
        Task.Run(() =>
        {
            var search = GetSearch(inputs);
            ExtensionHost.LogMessage(new LogMessage() { Message = $"Search: {search}" });
        });

        return CommandResult.KeepOpen();
    }

    public SearchCandidate GetSearch(string? payload)
    {
        try
        {
            if (string.IsNullOrEmpty(payload))
            {
                return new SearchCandidate();
            }

            var payloadJson = JsonNode.Parse(payload) ?? throw new InvalidOperationException("No search found");

            var search = CreateSearchFromJson(payloadJson);

            // if editing the search, delete the old one
            // it is safe to do as the new one is already validated

            // UpdateSearchTopLevelStatus adds the search if it's not already in the datastore
            LoadingStateChanged?.Invoke(this, false);
            _savedSearchesMediator.AddSearch(search);
            FormSubmitted?.Invoke(this, new FormSubmitEventArgs(true, null));
            return search;
        }
        catch (Exception ex)
        {
            LoadingStateChanged?.Invoke(this, false);
            _savedSearchesMediator.AddSearch(ex);
            FormSubmitted?.Invoke(this, new FormSubmitEventArgs(false, ex));
        }

        return new SearchCandidate();
    }

    public SearchCandidate CreateSearchFromJson(JsonNode? jsonNode)
    {
        var enteredSearch = jsonNode?["EnteredSearch"]?.ToString() ?? string.Empty;
        var name = jsonNode?["Name"]?.ToString() ?? string.Empty;
        var isTopLevel = jsonNode?["IsTopLevel"]?.ToString() == "true";

        if (!string.IsNullOrEmpty(enteredSearch) && !string.IsNullOrEmpty(name))
        {
            var azureUri = new AzureUri(enteredSearch);
            var queryInfo = AzureClientHelpers.GetQueryInfo(azureUri, _developerIdProvider.GetLoggedInDeveloperIdsInternal().FirstOrDefault()!);
            var selectedQueryId = queryInfo.AzureUri.Query;   // This will be empty string if invalid query.
        }

        return new SearchCandidate(enteredSearch, name, isTopLevel);
    }

    public bool GetIsTopLevel()
    {
        return false;
    }
}
