// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Nodes;
using AzureExtension.Account;
using AzureExtension.Client;
using AzureExtension.Controls.DataTransfer;
using AzureExtension.Helpers;

namespace AzureExtension.Controls.Forms;

public class SavePipelineSearchForm : AzureForm<IPipelineDefinitionSearch>
{
    private readonly IResources _resources;
    private readonly AzureClientHelpers _azureClientHelpers;
    private readonly IAccountProvider _accountProvider;

    public override Dictionary<string, string> TemplateSubstitutions => new()
    {
        { "{{SavePipelineSearchFormTitle}}", !string.IsNullOrEmpty(SavedSearch?.Url) ? _resources.GetResource("Forms_Edit_PipelineSearch") : _resources.GetResource("Forms_Save_PipelineSearch") },
        { "{{SavedPipelineSearchString}}", !string.IsNullOrEmpty(SavedSearch?.Url) ? SavedSearch!.Url : string.Empty }, // pipeline URL
        { "{{EnteredPipelineSearchErrorMessage}}", _resources.GetResource("Forms_SavePipelineSearch_TemplateEnteredPipelineSearchError") },
        { "{{EnteredPipelineSearchLabel}}", _resources.GetResource("Forms_SavePipelineSearch_TemplateEnteredPipelineSearchLabel") },
        { "{{Forms_SavePipelineSearch_URLPlaceholderSuffix}}", _resources.GetResource("Forms_SavePipelineSearch_URLPlaceholderSuffix") },
        { "{{IsTopLevelTitle}}", _resources.GetResource("Forms_SavePipelineSearch_TemplateIsTopLevelTitle") },
        { "{{IsTopLevel}}", IsTopLevelChecked },
        { "{{SavePipelineSearchActionTitle}}", _resources.GetResource("Forms_SavePipelineSearch_TemplateSavePipelineSearchActionTitle") },
    };

    public SavePipelineSearchForm(
        IPipelineDefinitionSearch? definitionSearch,
        IResources resources,
        ISavedSearchesUpdater<IPipelineDefinitionSearch> definitionRepository,
        SavedAzureSearchesMediator mediator,
        IAccountProvider accountProvider,
        AzureClientHelpers azureClientHelpers)
        : base(definitionSearch, definitionRepository, mediator, accountProvider)
    {
        _resources = resources;
        _azureClientHelpers = azureClientHelpers;
        _accountProvider = accountProvider;
        TemplateKey = "SavePipelineSearch";
    }

    // Creates a DefinitionSearch based on a URL in the following format
    // https://dev.azure.com/microsoft/project/_build?definitionId=definitionId
    protected override IPipelineDefinitionSearch CreateSearchFromJson(JsonNode jsonNode)
    {
        var definitionUrl = jsonNode?["EnteredPipelineSearch"]?.ToString() ?? string.Empty;
        var isTopLevel = jsonNode?["IsTopLevel"]?.ToString() == "true";

        var account = _accountProvider.GetDefaultAccount();
        var definitionId = ParseDefinitionIdFromUrl(definitionUrl);
        var definitionInfo = _azureClientHelpers.GetInfo(new AzureUri(definitionUrl), account, InfoType.Definition, definitionId).Result;

        if (definitionInfo.Result != ResultType.Success)
        {
            throw new InvalidOperationException($"Failed to get query info {definitionInfo.Error}: {definitionInfo.ErrorMessage}");
        }

        var uri = definitionInfo.AzureUri;
        return new PipelineDefinitionSearchCandidate
        {
            InternalId = definitionId,
            Url = uri.ToString(),
            IsTopLevel = isTopLevel,
        };
    }

    private long ParseDefinitionIdFromUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("URL cannot be null or empty.", nameof(url));
        }

        try
        {
            var uri = new Uri(url);
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            var definitionId = query["definitionId"];

            if (string.IsNullOrEmpty(definitionId) || !long.TryParse(definitionId, out var id))
            {
                throw new InvalidOperationException("The URL does not contain a valid definitionId.");
            }

            return id;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to parse definitionId from the URL.", ex);
        }
    }
}
