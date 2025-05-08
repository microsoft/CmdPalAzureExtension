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
    private readonly ISavedSearchesUpdater<IPullRequestSearch> _pullRequestSearchRepository;
    private readonly IPullRequestSearch _savedPullRequestSearch;
    private readonly IAccountProvider _accountProvider;

    public event EventHandler<bool>? LoadingStateChanged;

    public event EventHandler<FormSubmitEventArgs>? FormSubmitted;

    private string IsTopLevelChecked => GetIsTopLevel().ToString().ToLower(CultureInfo.InvariantCulture);

    public Dictionary<string, string> TemplateSubstitutions => new()
    {
        { "{{RepositoryUrlPlaceholder}}", _resources.GetResource("Forms_SavePullRequestSearch_TemplateRepositoryUrlPlaceholder") },
        { "{{RepositoryUrlLabel}}", _resources.GetResource("Forms_SavePullRequestSearch_TemplateRepositoryUrlLabel") },
        { "{{PullRequestSearchRepositoryUrl}}", _savedPullRequestSearch.Url },
        { "{{RepositoryUrlError}}", _resources.GetResource("Forms_SavePullRequestSearch_TemplateRepositoryUrlError") },
        { "{{PullRequestSearchTitlePlaceholder}}", _resources.GetResource("Forms_SavePullRequestSearch_TemplatePullRequestSearchTitlePlaceholder") },
        { "{{PullRequestSearchTitleLabel}}", _resources.GetResource("Forms_SavePullRequestSearch_TemplatePullRequestSearchTitleLabel") },
        { "{{EnteredPullRequestSearchTitle}}", _savedPullRequestSearch.Name },
        { "{{PullRequestSearchViewMineTitle}}", _resources.GetResource("Forms_SavePullRequestSearch_TemplateViewMineTitle") },
        { "{{PullRequestSearchViewAssignedToMeTitle}}", _resources.GetResource("Forms_SavePullRequestSearch_TemplateViewAssignedToMeTitle") },
        { "{{PullRequestSearchViewAllTitle}}", _resources.GetResource("Forms_SavePullRequestSearch_TemplateViewAllTitle") },
        { "{{PullRequestSearchSelectedView}}", string.IsNullOrEmpty(_savedPullRequestSearch.View) ? _resources.GetResource("Forms_SavePullRequestSearch_TemplateDefaultView") : _savedPullRequestSearch.View },
        { "{{IsTopLevelTitle}}", _resources.GetResource("Forms_SavePullRequestSearch_TemplateIsTopLevelTitle") },
        { "{{IsTopLevel}}", IsTopLevelChecked },
        { "{{SavePullRequestSearchActionTitle}}", _resources.GetResource("Forms_SavePullRequestSearch_TemplateSavePullRequestSearchActionTitle") },
    };

    public override string TemplateJson => TemplateHelper.LoadTemplateJsonFromTemplateName("SavePullRequestSearch", TemplateSubstitutions);

    // for saving a new pull request search
    public SavePullRequestSearchForm(
        IResources resources,
        SavedAzureSearchesMediator mediator,
        IAccountProvider accountProvider,
        ISavedSearchesUpdater<IPullRequestSearch> pullRequestSearchRepository)
    {
        _resources = resources;
        _mediator = mediator;
        _accountProvider = accountProvider;
        _pullRequestSearchRepository = pullRequestSearchRepository;
        _savedPullRequestSearch = new PullRequestSearch();
    }

    // for editing an existing pull request search
    public SavePullRequestSearchForm(
        IPullRequestSearch savedPullRequestSearch,
        IResources resources,
        SavedAzureSearchesMediator mediator,
        IAccountProvider accountProvider,
        ISavedSearchesUpdater<IPullRequestSearch> pullRequestSearchRepository)
    {
        _resources = resources;
        _mediator = mediator;
        _accountProvider = accountProvider;
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

                _pullRequestSearchRepository.RemoveSavedSearch(_savedPullRequestSearch);
            }

            LoadingStateChanged?.Invoke(this, false);

            await _pullRequestSearchRepository.AddOrUpdateSearch(pullRequestSearch, pullRequestSearch.IsTopLevel, _accountProvider.GetDefaultAccount());

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
        var searchUri = new AzureUri(searchUrl);
        var name = jsonNode?["title"]?.ToString() ?? string.Empty;
        var view = jsonNode?["view"]?.ToString() ?? string.Empty;
        var isTopLevel = jsonNode?["IsTopLevel"]?.ToString() == "true";

        if (string.IsNullOrEmpty(name))
        {
            name = $"{searchUri.Repository} - {view}";
        }

        return new PullRequestSearch(searchUri, name, view, isTopLevel);
    }

    public bool GetIsTopLevel()
    {
        return _pullRequestSearchRepository.IsTopLevel(_savedPullRequestSearch);
    }
}
