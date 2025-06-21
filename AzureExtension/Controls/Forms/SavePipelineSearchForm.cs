// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Nodes;
using AzureExtension.Account;
using AzureExtension.Client;
using AzureExtension.Controls.Commands;
using AzureExtension.Controls.DataTransfer;
using AzureExtension.Helpers;
using Serilog;

namespace AzureExtension.Controls.Forms;

public class SavePipelineSearchForm : SaveSearchForm<IPipelineDefinitionSearch>
{
    private readonly IResources _resources;

    private string _definitionUrl = string.Empty;

    private bool _isNewSearchTopLevel;

    private ILogger _logger;

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

    // Creates a DefinitionSearch based on a URL in the following format
    // https://dev.azure.com/microsoft/project/_build?definitionId=definitionId
    public SavePipelineSearchForm(
        IPipelineDefinitionSearch? definitionSearch,
        IResources resources,
        ISavedSearchesUpdater<IPipelineDefinitionSearch> definitionRepository,
        SavedAzureSearchesMediator mediator,
        IAccountProvider accountProvider,
        AzureClientHelpers azureClientHelpers,
        SaveSearchCommand<IPipelineDefinitionSearch> saveSearchCommand)
        : base(definitionSearch, definitionRepository, mediator, accountProvider, saveSearchCommand, resources, azureClientHelpers)
    {
        _resources = resources;
        TemplateKey = "SavePipelineSearch";
        _logger = Log.Logger.ForContext("SourceContext", nameof(SavePipelineSearchForm));
        _logger.Information($"SavePipelineSearchForm: Initialized with saved search: {definitionSearch?.Name ?? "null"} SavedSearch: {SavedSearch}");
    }

    protected override void ParseFormSubmission(JsonNode? jsonNode)
    {
        _definitionUrl = jsonNode?["EnteredPipelineSearch"]?.ToString() ?? string.Empty;
        _isNewSearchTopLevel = jsonNode?["IsTopLevel"]?.ToString() == "true";
    }

    protected override IPipelineDefinitionSearch CreateSearchFromSearchInfo(InfoResult searchInfo)
    {
        var definitionId = ParseDefinitionIdFromUrl(_definitionUrl);

        var uri = searchInfo.AzureUri;
        return new PipelineDefinitionSearchCandidate
        {
            InternalId = definitionId,
            Url = uri.ToString(),
            IsTopLevel = _isNewSearchTopLevel,
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

    protected override SearchInfoParameters GetSearchInfoParameters()
    {
        return new DefinitionInfoParameters(_definitionUrl, ParseDefinitionIdFromUrl(_definitionUrl));
    }
}
