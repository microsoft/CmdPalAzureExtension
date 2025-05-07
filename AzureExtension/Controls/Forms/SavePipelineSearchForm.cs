// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Nodes;
using AzureExtension.Account;
using AzureExtension.Client;
using AzureExtension.Helpers;
using AzureExtension.PersistentData;
using Serilog;

namespace AzureExtension.Controls.Forms;

public class SavePipelineSearchForm : AzureForm, IAzureForm
{
    private readonly IResources _resources;

    private readonly IDefinitionSearch _savedDefinitionSearch;

    private readonly IDefinitionRepository _definitionRepository;

    private readonly SavedAzureSearchesMediator _mediator;

    private readonly IAccountProvider _accountProvider;

    private readonly AzureClientHelpers _azureClientHelpers;

    public event EventHandler<bool>? LoadingStateChanged;

    public event EventHandler<FormSubmitEventArgs>? FormSubmitted;

    private string IsTopLevelChecked => "false";

    public SavePipelineSearchForm(
        IResources resources,
        IDefinitionRepository definitionRepository,
        SavedAzureSearchesMediator mediator,
        IAccountProvider accountProvider,
        AzureClientHelpers azureClientHelpers)
    {
        _resources = resources;
        _savedDefinitionSearch = new DefinitionSearch();
        _definitionRepository = definitionRepository;
        _mediator = mediator;
        _accountProvider = accountProvider;
        _azureClientHelpers = azureClientHelpers;
        TemplateKey = "SavePipelineSearch";
        TemplateSubstitutions = new Dictionary<string, string>()
        {
            { "{{SavePipelineSearchFormTitle}}", _resources.GetResource(string.IsNullOrEmpty(string.Empty) ? "Forms_Save_PipelineSearch" : "Forms_Edit_PipelineSearch") },
            { "{{SavedPipelineSearchString}}", string.Empty }, // pipeline URL
            { "{{EnteredPipelineSearchErrorMessage}}", _resources.GetResource("Forms_SavePipelineSearch_TemplateEnteredPipelineSearchError") },
            { "{{EnteredPipelineSearchLabel}}", _resources.GetResource("Forms_SavePipelineSearch_TemplateEnteredPipelineSearchLabel") },
            { "{{Forms_SavePipelineSearch_URLPlaceholderSuffix}}", _resources.GetResource("Forms_SavePipelineSearch_URLPlaceholderSuffix") },
            { "{{IsTopLevelTitle}}", _resources.GetResource("Forms_SavePipelineSearch_TemplateIsTopLevelTitle") },
            { "{{IsTopLevel}}", IsTopLevelChecked },
            { "{{SavePipelineSearchActionTitle}}", _resources.GetResource("Forms_SavePipelineSearch_TemplateSavePipelineSearchActionTitle") },
        };
    }

    public SavePipelineSearchForm(
        IDefinitionSearch definitionSearch,
        IResources resources,
        IDefinitionRepository definitionRepository,
        SavedAzureSearchesMediator mediator,
        IAccountProvider accountProvider,
        AzureClientHelpers azureClientHelpers)
    {
        _resources = resources;
        _savedDefinitionSearch = definitionSearch;
        _definitionRepository = definitionRepository;
        _mediator = mediator;
        _accountProvider = accountProvider;
        _azureClientHelpers = azureClientHelpers;
        TemplateKey = "SavePipelineSearch";
        TemplateSubstitutions = new Dictionary<string, string>()
        {
            { "{{SavePipelineSearchFormTitle}}", _resources.GetResource(string.IsNullOrEmpty(string.Empty) ? "Forms_Save_PipelineSearch" : "Forms_Edit_PipelineSearch") },
            { "{{SavedPipelineSearchString}}", string.Empty }, // pipeline URL
            { "{{EnteredPipelineSearchErrorMessage}}", _resources.GetResource("Forms_SavePipelineSearch_TemplateEnteredPipelineSearchError") },
            { "{{EnteredPipelineSearchLabel}}", _resources.GetResource("Forms_SavePipelineSearch_TemplateEnteredPipelineSearchLabel") },
            { "{{Forms_SavePipelineSearch_URLPlaceholderSuffix}}", _resources.GetResource("Forms_SavePipelineSearch_URLPlaceholderSuffix") },
            { "{{IsTopLevelTitle}}", _resources.GetResource("Forms_SavePipelineSearch_TemplateIsTopLevelTitle") },
            { "{{IsTopLevel}}", IsTopLevelChecked },
            { "{{SavePipelineSearchActionTitle}}", _resources.GetResource("Forms_SavePipelineSearch_TemplateSavePipelineSearchActionTitle") },
        };
    }

    public override Task HandleInputs(string inputs)
    {
        LoadingStateChanged?.Invoke(this, true);

        try
        {
            var payloadJson = JsonNode.Parse(inputs) ?? throw new InvalidOperationException("No search found");

            var pipelineSearch = CreatePipelineSearchFromJson(payloadJson);

            // if editing the search, delete the old one
            // it is safe to do as the new one is already validated
            if (_savedDefinitionSearch.ProjectUrl != string.Empty)
            {
                var definition = _definitionRepository.GetDefinition(_savedDefinitionSearch, _accountProvider.GetDefaultAccount()).Result;
                Log.Information($"Removing outdated search {definition.Name}, {definition.HtmlUrl}");

                _definitionRepository.RemoveSavedDefinitionSearch(_savedDefinitionSearch);
            }

            LoadingStateChanged?.Invoke(this, false);
            _definitionRepository.UpdateDefinitionSearchTopLevelStatus(pipelineSearch, pipelineSearch.IsTopLevel, _accountProvider.GetDefaultAccount());
            _mediator.AddPipelineSearch(pipelineSearch);
            FormSubmitted?.Invoke(this, new FormSubmitEventArgs(true, null));
        }
        catch (Exception ex)
        {
            LoadingStateChanged?.Invoke(this, false);
            _mediator.AddPipelineSearch(ex);
            FormSubmitted?.Invoke(this, new FormSubmitEventArgs(false, ex));
        }

        return Task.CompletedTask;
    }

    // Creates a DefinitionSearch based on a URL in the following format
    // https://dev.azure.com/microsoft/project/_build?definitionId=definitionId
    public DefinitionSearch CreatePipelineSearchFromJson(JsonNode? jsonNode)
    {
        var definitionUrl = jsonNode?["EnteredPipelineSearch"]?.ToString() ?? string.Empty;
        var isTopLevel = jsonNode?["IsTopLevel"]?.ToString() == "true";

        var account = _accountProvider.GetDefaultAccount();
        var definitionId = ParseDefinitionIdFromUrl(definitionUrl);
        var definitionInfo = _azureClientHelpers.GetInfo(new AzureUri(definitionUrl), account, InfoType.Definition, definitionId).Result;

        if (definitionInfo.Result != ResultType.Success)
        {
            var error = definitionInfo.Error;
            throw new InvalidOperationException($"Failed to get query info {definitionInfo.Error}: {definitionInfo.ErrorMessage}");
        }

        var uri = definitionInfo.AzureUri;
        return new DefinitionSearch
        {
            InternalId = definitionId,
            ProjectUrl = uri.ToString(),
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
