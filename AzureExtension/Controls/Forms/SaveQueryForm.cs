// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Nodes;
using AzureExtension.Account;
using AzureExtension.Client;
using AzureExtension.Controls.Commands;
using AzureExtension.Helpers;

namespace AzureExtension.Controls.Forms;

public sealed partial class SaveQueryForm : SaveSearchForm<IQuerySearch>
{
    private readonly IResources _resources;
    private bool _isNewSearchTopLevel;
    private string _searchUrl = string.Empty;
    private string _displayName = string.Empty;

    public override Dictionary<string, string> TemplateSubstitutions => new()
    {
        { "{{SaveQueryFormTitle}}", _resources.GetResource(string.IsNullOrEmpty(SavedSearch?.Name) ? "Forms_Save_Query" : "Forms_Edit_Query") },
        { "{{SavedQueryString}}", SavedSearch?.Url ?? string.Empty },
        { "{{EnteredQueryErrorMessage}}", _resources.GetResource("Forms_SaveQuery_TemplateEnteredQueryError") },
        { "{{EnteredQueryLabel}}", _resources.GetResource("Forms_SaveQuery_TemplateEnteredQueryLabel") },
        { "{{QueryDisplayNameLabel}}", _resources.GetResource("Forms_SaveQuery_TemplateQueryDisplayNameLabel") },
        { "{{QueryDisplayName}}", SavedSearch?.Name ?? string.Empty },
        { "{{QueryDisplayNamePlaceholder}}", _resources.GetResource("Forms_SaveQuery_TemplateQueryDisplayNamePlaceholder") },
        { "{{IsTopLevelTitle}}", _resources.GetResource("Forms_SaveQueryTemplate_IsTopLevelTitle") },
        { "{{IsTopLevel}}", IsTopLevelChecked },
        { "{{SaveQueryActionTitle}}", _resources.GetResource("Forms_SaveQuery_TemplateSaveQueryActionTitle") },
    };

    // if savedQuery is null, the form will save a new query search
    // otherwise, it will edit an existing query search
    public SaveQueryForm(
        IQuerySearch? savedQuery,
        IResources resources,
        SavedAzureSearchesMediator savedQueriesMediator,
        IAccountProvider accountProvider,
        AzureClientHelpers azureClientHelpers,
        ISavedSearchesUpdater<IQuerySearch> queryRepository,
        SaveSearchCommand<IQuerySearch> saveSearchCommand)
        : base(savedQuery, queryRepository, savedQueriesMediator, accountProvider, saveSearchCommand, resources, azureClientHelpers)
    {
        _resources = resources;
        TemplateKey = "SaveQuery";
    }

    protected override void ParseFormSubmission(JsonNode? jsonNode)
    {
        _searchUrl = jsonNode?["EnteredQuery"]?.ToString() ?? string.Empty;
        _displayName = jsonNode?["QueryDisplayName"]?.ToString() ?? string.Empty;
        _isNewSearchTopLevel = jsonNode?["IsTopLevel"]?.ToString() == "true";
    }

    protected override IQuerySearch CreateSearchFromSearchInfo(InfoResult searchInfo)
    {
        var name = !string.IsNullOrEmpty(_displayName) ? _displayName : searchInfo.Name;
        return new Query(searchInfo.AzureUri, name, searchInfo.Description, _isNewSearchTopLevel);
    }

    protected override SearchInfoParameters GetSearchInfoParameters()
    {
        return new DefaultSearchInfoParameters(_searchUrl, InfoType.Query);
    }
}
