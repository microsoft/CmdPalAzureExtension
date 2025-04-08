// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Text.Json.Nodes;
using AzureExtension.DeveloperId;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Serilog;

namespace AzureExtension.Controls.Forms;

public sealed partial class SaveSearchForm : FormContent, IAzureForm
{
    private readonly ISearch _savedSearch;

    private readonly IResources _resources;

    private readonly SavedSearchesMediator _savedSearchesMediator;

    private readonly IDeveloperIdProvider _developerIdProvider;

    private readonly ISearchRepository _searchRepository;

    public event EventHandler<bool>? LoadingStateChanged;

    private string IsTopLevelChecked => GetIsTopLevel().Result.ToString().ToLower(CultureInfo.InvariantCulture);

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
    public SaveSearchForm(IResources resources, SavedSearchesMediator savedSearchesMediator, IDeveloperIdProvider developerIdProvider, ISearchRepository searchRepository)
    {
        _resources = resources;
        _savedSearch = new SearchCandidate();
        _savedSearchesMediator = savedSearchesMediator;
        _developerIdProvider = developerIdProvider;
        _searchRepository = searchRepository;
    }

    // for editing an existing query
    public SaveSearchForm(ISearch savedSearch, IResources resources, SavedSearchesMediator savedSearchesMediator, IDeveloperIdProvider developerIdProvider, ISearchRepository searchRepository)
    {
        _resources = resources;
        _savedSearch = savedSearch;
        _savedSearchesMediator = savedSearchesMediator;
        _developerIdProvider = developerIdProvider;
        _searchRepository = searchRepository;
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

    public async Task<QueryCandidate> GetSearch(string payload)
    {
        try
        {
            var payloadJson = JsonNode.Parse(payload) ?? throw new InvalidOperationException("No search found");

            var query = CreateQueryFromJson(payloadJson);

            var devId = _developerIdProvider.GetLoggedInDeveloperIds().DeveloperIds.FirstOrDefault()!;

            // if editing the search, delete the old one
            // it is safe to do as the new one is already validated
            if (_savedSearch.SearchString != string.Empty)
            {
                Log.Information($"Removing outdated search {_savedSearch.Name}, {_savedSearch.SearchString}");

                // Remove deleted search from top-level commands
                _searchRepository.UpdateSearchTopLevelStatus(_savedSearch, false, devId);
                await _searchRepository.RemoveSavedSearch(_savedSearch);
            }

            // UpdateSearchTopLevelStatus adds the search if it's not already in the datastore
            _searchRepository.UpdateSearchTopLevelStatus(query, query.IsTopLevel, devId);

            LoadingStateChanged?.Invoke(this, false);
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

        return new QueryCandidate();
    }

    public async Task<bool> GetIsTopLevel()
    {
        var getTopLevel = await _searchRepository.IsTopLevel(_savedSearch);
        return getTopLevel;
    }

    public QueryCandidate CreateQueryFromJson(JsonNode? jsonNode)
    {
        var queryUrl = jsonNode?["EnteredSearch"]?.ToString() ?? string.Empty;
        var name = jsonNode?["Name"]?.ToString() ?? string.Empty;
        var isTopLevel = jsonNode?["IsTopLevel"]?.ToString() == "true";

        var developerId = _developerIdProvider.GetLoggedInDeveloperIds().DeveloperIds.FirstOrDefault()!;
        var queryInfo = SearchValidator.GetQueryInfo(queryUrl, name, developerId);

        if (string.IsNullOrEmpty(name))
        {
            name = queryInfo.Name;
        }

        // Create a QueryCandidate with the required information
        return new QueryCandidate(
            displayName: name,
            queryId: queryInfo.AzureUri?.Query ?? string.Empty,
            searchString: queryUrl,
            projectId: 0, // Will be filled in by the repository later
            developerLogin: developerId.LoginId,
            queryResults: string.Empty, // Will be filled in by the repository later
            queryResultCount: 0, // Will be filled in by the repository later
            isTopLevel: isTopLevel,
            azureUri: queryInfo.AzureUri);
    }
}
