// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Text.Json.Nodes;
using AzureExtension.Account;
using AzureExtension.Client;
using AzureExtension.DataModel;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Serilog;
using static System.Net.WebRequestMethods;

namespace AzureExtension.Controls.Forms;

public class SavePullRequestSearchForm : FormContent, IAzureForm
{
    private readonly IResources _resources;
    private readonly SavedAzureSearchesMediator _mediator;
    private readonly ISavedSearchesUpdater<IPullRequestSearch> _pullRequestSearchRepository;
    private readonly IAccountProvider _accountProvider;
    private IPullRequestSearch? _savedPullRequestSearch;

    public event EventHandler<bool>? LoadingStateChanged;

    public event EventHandler<FormSubmitEventArgs>? FormSubmitted;

    private string IsTopLevelChecked => GetIsTopLevel().ToString().ToLower(CultureInfo.InvariantCulture);

    public Dictionary<string, string> TemplateSubstitutions => new()
    {
        { "{{RepositoryUrlPlaceholder}}", _resources.GetResource("Forms_SavePullRequestSearch_TemplateRepositoryUrlPlaceholder") },
        { "{{RepositoryUrlLabel}}", _resources.GetResource("Forms_SavePullRequestSearch_TemplateRepositoryUrlLabel") },
        { "{{PullRequestSearchRepositoryUrl}}", _savedPullRequestSearch?.Url ?? string.Empty },
        { "{{RepositoryUrlError}}", _resources.GetResource("Forms_SavePullRequestSearch_TemplateRepositoryUrlError") },
        { "{{PullRequestSearchTitlePlaceholder}}", _resources.GetResource("Forms_SavePullRequestSearch_TemplatePullRequestSearchTitlePlaceholder") },
        { "{{PullRequestSearchTitleLabel}}", _resources.GetResource("Forms_SavePullRequestSearch_TemplatePullRequestSearchTitleLabel") },
        { "{{EnteredPullRequestSearchTitle}}", _savedPullRequestSearch?.Name ?? string.Empty },
        { "{{PullRequestSearchViewMineTitle}}", _resources.GetResource("Forms_SavePullRequestSearch_TemplateViewMineTitle") },
        { "{{PullRequestSearchViewAssignedToMeTitle}}", _resources.GetResource("Forms_SavePullRequestSearch_TemplateViewAssignedToMeTitle") },
        { "{{PullRequestSearchViewAllTitle}}", _resources.GetResource("Forms_SavePullRequestSearch_TemplateViewAllTitle") },
        { "{{PullRequestSearchSelectedView}}", string.IsNullOrEmpty(_savedPullRequestSearch?.View) ? _resources.GetResource("Forms_SavePullRequestSearch_TemplateDefaultView") : _savedPullRequestSearch.View },
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
        _savedPullRequestSearch = null;
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

            await _pullRequestSearchRepository.Validate(pullRequestSearch, _accountProvider.GetDefaultAccount());

            // if editing the search, delete the old one
            // it is safe to do as the new one is already validated
            if (_savedPullRequestSearch != null)
            {
                Log.Information($"Removing outdated search {_savedPullRequestSearch.Name}, {_savedPullRequestSearch.Url}");

                _pullRequestSearchRepository.RemoveSavedSearch(_savedPullRequestSearch);
            }

            LoadingStateChanged?.Invoke(this, false);
            _pullRequestSearchRepository.AddOrUpdateSearch(pullRequestSearch, pullRequestSearch.IsTopLevel);
            _mediator.AddPullRequestSearch(pullRequestSearch);

            if (_savedPullRequestSearch != null)
            {
                _savedPullRequestSearch = pullRequestSearch;
            }

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
        var enteredUrl = jsonNode?["url"]?.ToString() ?? string.Empty;
        var name = jsonNode?["title"]?.ToString() ?? string.Empty;
        var view = jsonNode?["view"]?.ToString() ?? string.Empty;
        var isTopLevel = jsonNode?["IsTopLevel"]?.ToString() == "true";

        var testUri = new AzureUri(enteredUrl);
        var url = CreatePullRequestUrl(testUri, view);
        var searchUri = new AzureUri(url);
        if (string.IsNullOrEmpty(name) || !string.Equals(name, $"{searchUri.Repository} - {_savedPullRequestSearch?.View}", StringComparison.OrdinalIgnoreCase))
        {
            name = $"{searchUri.Repository} - {view}";
        }

        return new PullRequestSearch(searchUri, name, view, isTopLevel);
    }

    public bool GetIsTopLevel()
    {
        if (_savedPullRequestSearch == null)
        {
            return false;
        }

        return _pullRequestSearchRepository.IsTopLevel(_savedPullRequestSearch!);
    }

    // The form enforces that the URL is not null or empty, so we can assume it is valid
    // This assumes the URL is for a repository, not the list of pull requests
    public string CreatePullRequestUrl(AzureUri uri, string? view)
    {
        // the View values are hardcoded in SavePullRequestSearchForm.json
        var enteredViewToUrlView = new Dictionary<string, string>()
        {
            { "All", "active" },
            { "Assigned", "mine" },
            { "Mine", "mine" },
        };

        if (!enteredViewToUrlView.TryGetValue(view ?? _resources.GetResource("Forms_SavePullRequestSearch_TemplateViewAllTitle"), out var viewValue))
        {
            viewValue = "active";
        }

        try
        {
            var baseUrl = $"https://dev.azure.com/{uri.Organization.ToString()}/{uri.Project.ToString()}/_git/{uri.Repository.ToString()}".TrimEnd('/');

            return $"{baseUrl}/pullrequests?_a={viewValue}";
        }
        catch (UriFormatException ex)
        {
            throw new FormatException("The provided URL is not valid.", ex);
        }
    }
}
