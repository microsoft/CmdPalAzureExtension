// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Nodes;
using AzureExtension.Account;
using AzureExtension.Client;
using AzureExtension.Controls.Commands;
using AzureExtension.Helpers;
using Serilog;

namespace AzureExtension.Controls.Forms;

public class SavePullRequestSearchForm : SaveSearchForm<IPullRequestSearch>
{
    private readonly IResources _resources;

    private string _repoUrl = string.Empty;

    private string _view = string.Empty;

    private bool _isNewSearchTopLevel;

    private ILogger _logger;

    public override Dictionary<string, string> TemplateSubstitutions => new()
    {
        { "{{RepositoryUrlPlaceholder}}", _resources.GetResource("Forms_SavePullRequestSearch_TemplateRepositoryUrlPlaceholder") },
        { "{{RepositoryUrlLabel}}", _resources.GetResource("Forms_SavePullRequestSearch_TemplateRepositoryUrlLabel") },
        { "{{PullRequestSearchRepositoryUrl}}", SavedSearch?.Url ?? string.Empty },
        { "{{RepositoryUrlError}}", _resources.GetResource("Forms_SavePullRequestSearch_TemplateRepositoryUrlError") },
        { "{{PullRequestSearchViewMineTitle}}", _resources.GetResource("Forms_SavePullRequestSearch_TemplateViewMineTitle") },
        { "{{PullRequestSearchViewAssignedToMeTitle}}", _resources.GetResource("Forms_SavePullRequestSearch_TemplateViewAssignedToMeTitle") },
        { "{{PullRequestSearchViewAllTitle}}", _resources.GetResource("Forms_SavePullRequestSearch_TemplateViewAllTitle") },
        { "{{PullRequestSearchSelectedView}}", string.IsNullOrEmpty(SavedSearch?.View) ? _resources.GetResource("Forms_SavePullRequestSearch_TemplateViewDefault") : SavedSearch.View },
        { "{{IsTopLevelTitle}}", _resources.GetResource("Forms_SavePullRequestSearch_TemplateIsTopLevelTitle") },
        { "{{IsTopLevel}}", IsTopLevelChecked },
        { "{{SavePullRequestSearchActionTitle}}", _resources.GetResource("Forms_SavePullRequestSearch_TemplateSavePullRequestSearchActionTitle") },
    };

    // for editing an existing pull request search
    public SavePullRequestSearchForm(
        IPullRequestSearch? savedPullRequestSearch,
        IResources resources,
        SavedAzureSearchesMediator mediator,
        IAccountProvider accountProvider,
        AzureClientHelpers azureClientHelpers,
        ISavedSearchesUpdater<IPullRequestSearch> pullRequestSearchRepository,
        SaveSearchCommand<IPullRequestSearch> saveSearchCommand)
        : base(savedPullRequestSearch, pullRequestSearchRepository, mediator, accountProvider, saveSearchCommand, resources, azureClientHelpers)
    {
        _resources = resources;
        TemplateKey = "SavePullRequestSearch";
        _logger = Log.Logger.ForContext("SourceContext", nameof(SavePullRequestSearchForm));
        _logger.Debug($"SavePullRequestSearchForm: Initialized with saved query: {savedPullRequestSearch?.Name ?? "null"} SavedSearch: {SavedSearch}");
    }

    protected override void ParseFormSubmission(JsonNode? jsonNode)
    {
        _repoUrl = jsonNode?["url"]?.ToString() ?? string.Empty;
        _view = jsonNode?["view"]?.ToString() ?? string.Empty;
        _isNewSearchTopLevel = jsonNode?["IsTopLevel"]?.ToString() == "true";
    }

    protected override IPullRequestSearch CreateSearchFromSearchInfo(InfoResult searchInfo)
    {
        _logger.Debug($"SavePullRequestSearchForm: Creating PullRequestSearch with searchInfo: {searchInfo}, {searchInfo.Name}");
        var name = $"{searchInfo.Name} - {_view}";
        var pullRequestsUri = new AzureUri(CreatePullRequestUrl(searchInfo.AzureUri, _view));

        return new PullRequestSearchCandidate(pullRequestsUri, name, _view, _isNewSearchTopLevel);
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
            var baseUrl = $"https://dev.azure.com/{uri.Organization}/{uri.Project}/_git/{uri.Repository}".TrimEnd('/');

            return $"{baseUrl}/pullrequests?_a={viewValue}";
        }
        catch (UriFormatException ex)
        {
            throw new FormatException("The provided URL is not valid.", ex);
        }
    }

    // In order to validate the pull request URL, we need to use the repository URL
    protected override SearchInfoParameters GetSearchInfoParameters()
    {
        return new DefaultSearchInfoParameters(_repoUrl, InfoType.Repository);
    }
}
