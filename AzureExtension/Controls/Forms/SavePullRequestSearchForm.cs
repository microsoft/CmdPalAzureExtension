// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Text.Json.Nodes;
using AzureExtension.Account;
using AzureExtension.Client;
using AzureExtension.Helpers;
using AzureExtension.PersistentData;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Serilog;

namespace AzureExtension.Controls.Forms;

public class SavePullRequestSearchForm : FormContent, IAzureForm
{
    private readonly IResources _resources;
    private readonly SavedAzureSearchesMediator _mediator;
    private readonly IAccountProvider _accountProvider;
    private readonly AzureClientHelpers _azureClientHelpers;
    private readonly ISavedPullRequestSearchRepository _pullRequestSearchRepository;
    private readonly IPullRequestSearch _savedPullRequestSearch;

    public event EventHandler<bool>? LoadingStateChanged;

    public event EventHandler<FormSubmitEventArgs>? FormSubmitted;

    private string IsTopLevelChecked => GetIsTopLevel().Result.ToString().ToLower(CultureInfo.InvariantCulture);

    public Dictionary<string, string> TemplateSubstitutions => new()
    {
        { "{{url}}", _savedPullRequestSearch.Url },
        { "{{enteredTitle}}", _savedPullRequestSearch.Name },
        { "{{selectedView}}", string.IsNullOrEmpty(_savedPullRequestSearch.View) ? "Mine" : _savedPullRequestSearch.View },
        { "{{IsTopLevelTitle}}", _resources.GetResource("Forms_SaveSearchTemplateIsTopLevelTitle") },
        { "{{IsTopLevel}}", IsTopLevelChecked },
    };

    public override string TemplateJson => TemplateHelper.LoadTemplateJsonFromTemplateName("SavePullRequestSearch", TemplateSubstitutions);

    // for saving a new pull request search
    public SavePullRequestSearchForm(IResources resources, SavedAzureSearchesMediator mediator, IAccountProvider accountProvider, AzureClientHelpers azureClientHelpers, ISavedPullRequestSearchRepository pullRequestSearchRepository)
    {
        _resources = resources;
        _mediator = mediator;
        _accountProvider = accountProvider;
        _azureClientHelpers = azureClientHelpers;
        _pullRequestSearchRepository = pullRequestSearchRepository;
        _savedPullRequestSearch = new PullRequestSearch();
    }

    // for editing an existing pull request search
    public SavePullRequestSearchForm(IPullRequestSearch savedPullRequestSearch, IResources resources, SavedAzureSearchesMediator mediator, IAccountProvider accountProvider, AzureClientHelpers azureClientHelpers, ISavedPullRequestSearchRepository pullRequestSearchRepository)
    {
        _resources = resources;
        _mediator = mediator;
        _accountProvider = accountProvider;
        _azureClientHelpers = azureClientHelpers;
        _pullRequestSearchRepository = pullRequestSearchRepository;
        _savedPullRequestSearch = savedPullRequestSearch;
    }

    public override ICommandResult SubmitForm(string inputs, string data)
    {
        LoadingStateChanged?.Invoke(this, true);
        Task.Run(async () =>
        {
            await AddPullRequestSearch(inputs);
        });

        return CommandResult.KeepOpen();
    }

    private async Task AddPullRequestSearch(string payload)
    {
        try
        {
            var payloadJson = JsonNode.Parse(payload) ?? throw new InvalidOperationException("No search found");

            var pullRequestSearch = CreatePullRequestSearchFromJson(payloadJson);

            // if editing the search, delete the old one
            // it is safe to do as the new one is already validated
            if (!string.IsNullOrEmpty(_savedPullRequestSearch.Url))
            {
                Log.Information($"Removing outdated search {_savedPullRequestSearch.Name}, {_savedPullRequestSearch.Url}");

                await _pullRequestSearchRepository.RemoveSavedPullRequestSearch(pullRequestSearch);
            }

            LoadingStateChanged?.Invoke(this, false);
            _pullRequestSearchRepository.UpdatePullRequestSearchTopLevelStatus(pullRequestSearch, pullRequestSearch.IsTopLevel);
            _mediator.AddPullRequestSearch(pullRequestSearch);
            FormSubmitted?.Invoke(this, new FormSubmitEventArgs(true, null));
        }
        catch (Exception ex)
        {
            LoadingStateChanged?.Invoke(this, false);
            _mediator.AddPullRequestSearch(ex);
            FormSubmitted?.Invoke(this, new FormSubmitEventArgs(false, ex));
        }
    }

    public PullRequestSearch CreatePullRequestSearchFromJson(JsonNode? jsonNode)
    {
        var searchUrl = jsonNode?["url"]?.ToString() ?? string.Empty;
        var name = jsonNode?["title"]?.ToString() ?? string.Empty;
        var view = jsonNode?["view"]?.ToString() ?? string.Empty;
        var isTopLevel = jsonNode?["IsTopLevel"]?.ToString() == "true";

        if (string.IsNullOrEmpty(name))
        {
            name = string.Empty;
        }

        return new PullRequestSearch(new AzureUri(searchUrl), name, view, isTopLevel);
    }

    public async Task<bool> GetIsTopLevel()
    {
        return await _pullRequestSearchRepository.IsTopLevel(_savedPullRequestSearch);
    }
}
