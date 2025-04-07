// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Text.Json.Nodes;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Forms;

public sealed partial class SaveSearchForm : FormContent, IAzureForm
{
    private readonly ISearch _savedSearch;

    private readonly IResources _resources;

    private readonly SavedSearchesMediator _savedSearchesMediator;

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
    public SaveSearchForm(IResources resources, SavedSearchesMediator savedSearchesMediator)
    {
        _resources = resources;
        _savedSearch = new SearchCandidate();
        _savedSearchesMediator = savedSearchesMediator;
    }

    // for editing an existing query
    public SaveSearchForm(ISearch savedSearch, IResources resources, SavedSearchesMediator savedSearchesMediator)
    {
        _resources = resources;
        _savedSearch = savedSearch;
        _savedSearchesMediator = savedSearchesMediator;
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

    public static SearchCandidate CreateSearchFromJson(JsonNode? jsonNode)
    {
        var enteredSearch = jsonNode?["EnteredSearch"]?.ToString() ?? string.Empty;
        var name = jsonNode?["Name"]?.ToString() ?? string.Empty;
        var isTopLevel = jsonNode?["IsTopLevel"]?.ToString() == "true";

        string? searchStr;

        searchStr = string.IsNullOrEmpty(enteredSearch) ? enteredSearch : string.Empty;

        return new SearchCandidate(searchStr, name, isTopLevel);
    }

    public bool GetIsTopLevel()
    {
        return false;
    }
}
