// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Nodes;
using AzureExtension.Account;
using AzureExtension.Client;
using AzureExtension.Helpers;

namespace AzureExtension.Controls.Forms;

public sealed partial class SaveQueryForm : AzureForm<IQuery>
{
    private readonly IResources _resources;
    private readonly IAccountProvider _accountProvider;
    private readonly AzureClientHelpers _azureClientHelpers;

    public override Dictionary<string, string> TemplateSubstitutions => new()
    {
        { "{{SaveQueryFormTitle}}", _resources.GetResource(string.IsNullOrEmpty(SavedSearch?.Name) ? "Forms_Save_Query" : "Forms_Edit_Query") },
        { "{{SavedQueryString}}", SavedSearch?.Url ?? string.Empty },
        { "{{EnteredQueryErrorMessage}}", _resources.GetResource("Forms_SaveQuery_TemplateEnteredQueryError") },
        { "{{EnteredQueryLabel}}", _resources.GetResource("Forms_SaveQuery_TemplateEnteredQueryLabel") },
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
        : base(null, queryRepository, savedQueriesMediator, accountProvider)
    {
        _resources = resources;
        _accountProvider = accountProvider;
        _azureClientHelpers = azureClientHelpers;
        TemplateKey = "SaveQuery";
    }

    // for editing an existing query
    public SaveQueryForm(
        IQuery savedQuery,
        IResources resources,
        SavedAzureSearchesMediator savedQueriesMediator,
        IAccountProvider accountProvider,
        AzureClientHelpers azureClientHelpers,
        ISavedSearchesUpdater<IQuery> queryRepository)
        : base(savedQuery, queryRepository, savedQueriesMediator, accountProvider)
    {
        _resources = resources;
        _accountProvider = accountProvider;
        _azureClientHelpers = azureClientHelpers;
        TemplateKey = "SaveQuery";
    }

    protected override IQuery CreateSearchFromJson(JsonNode? jsonNode)
    {
        var queryUrl = jsonNode?["EnteredQuery"]?.ToString() ?? string.Empty;
        var isTopLevel = jsonNode?["IsTopLevel"]?.ToString() == "true";

        var account = _accountProvider.GetDefaultAccount();
        var queryInfo = _azureClientHelpers.GetInfo(queryUrl, account, InfoType.Query).Result;

        if (queryInfo.Result != ResultType.Success)
        {
            var error = queryInfo.Error;
            throw new InvalidOperationException($"Failed to get query info {queryInfo.Error}: {queryInfo.ErrorMessage}");
        }

        var uri = queryInfo.AzureUri;
        return new Query(uri, queryInfo.Name, queryInfo.Description, isTopLevel);
    }
}
