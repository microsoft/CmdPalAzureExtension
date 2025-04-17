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
using Microsoft.Identity.Client;
using Serilog;

namespace AzureExtension.Controls.Forms;

public class SavePullRequestSearchForm : FormContent, IAzureForm
{
    private readonly IResources _resources;
    private readonly SavedQueriesMediator _mediator;
    private readonly IAccountProvider _accountProvider;
    private readonly AzureClientHelpers _azureClientHelpers;
    private readonly ISavedPullRequestSearchRepository _pullRequestSearchRepository;
    private readonly IPullRequestSearch _savedPullRequestSearch;

    public event EventHandler<bool>? LoadingStateChanged;

    public event EventHandler<FormSubmitEventArgs>? FormSubmitted;

    public Dictionary<string, string> TemplateSubstitutions => new()
    {
        { "{{url}}", _savedPullRequestSearch.Url },
        { "{{enteredTitle}}", _savedPullRequestSearch.Title },
        { "{{selectedView}}", string.IsNullOrEmpty(_savedPullRequestSearch.View) ? "Mine" : _savedPullRequestSearch.View },
    };

    public override string TemplateJson => TemplateHelper.LoadTemplateJsonFromTemplateName("SavePullRequestSearch", TemplateSubstitutions);

    // for saving a new pull request search
    public SavePullRequestSearchForm(IResources resources, SavedQueriesMediator mediator, IAccountProvider accountProvider, AzureClientHelpers azureClientHelpers, ISavedPullRequestSearchRepository pullRequestSearchRepository)
    {
        _resources = resources;
        _mediator = mediator;
        _accountProvider = accountProvider;
        _azureClientHelpers = azureClientHelpers;
        _pullRequestSearchRepository = pullRequestSearchRepository;
        _savedPullRequestSearch = new PullRequestSearch();
    }

    // for editing an existing pull request search
    public SavePullRequestSearchForm(IPullRequestSearch savedPullRequestSearch, IResources resources, SavedQueriesMediator mediator, IAccountProvider accountProvider, AzureClientHelpers azureClientHelpers, ISavedPullRequestSearchRepository pullRequestSearchRepository)
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
        Task.Run(() =>
        {
            var search = GetPullRequestSearch(inputs);
            ExtensionHost.LogMessage(new LogMessage() { Message = $"PullRequestSearch: {search}" });
        });

        return CommandResult.KeepOpen();
    }

    private PullRequestSearch GetPullRequestSearch(string payload)
    {
        try
        {
            var payloadJson = JsonNode.Parse(payload) ?? throw new InvalidOperationException("No search found");

            var pullRequestSearch = CreatePullRequestSearchFromJson(payloadJson);

            // if editing the search, delete the old one
            // it is safe to do as the new one is already validated
            if (!string.IsNullOrEmpty(_savedPullRequestSearch.Url))
            {
                Log.Information($"Removing outdated search {_savedPullRequestSearch.Title}, {_savedPullRequestSearch.Url}");

                _pullRequestSearchRepository.RemoveSavedPullRequestSearch(_savedPullRequestSearch).Wait();
            }

            LoadingStateChanged?.Invoke(this, false);
            _pullRequestSearchRepository.AddSavedPullRequestSearch(pullRequestSearch).Wait();
            _mediator.AddPullRequestSearch(pullRequestSearch);
            FormSubmitted?.Invoke(this, new FormSubmitEventArgs(true, null));
            return pullRequestSearch;
        }
        catch (Exception ex)
        {
            LoadingStateChanged?.Invoke(this, false);
            _mediator.AddPullRequestSearch(ex);
            FormSubmitted?.Invoke(this, new FormSubmitEventArgs(false, ex));
        }

        return new PullRequestSearch();
    }

    public PullRequestSearch CreatePullRequestSearchFromJson(JsonNode? jsonNode)
    {
        var searchUrl = jsonNode?["url"]?.ToString() ?? string.Empty;
        var name = jsonNode?["title"]?.ToString() ?? string.Empty;
        var view = jsonNode?["view"]?.ToString() ?? string.Empty;

        if (string.IsNullOrEmpty(name))
        {
            name = string.Empty;
        }

        return new PullRequestSearch(new AzureUri(searchUrl), name, view);
    }
}
