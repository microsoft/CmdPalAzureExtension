// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Text.Json.Nodes;
using AzureExtension.Account;
using AzureExtension.Client;
using AzureExtension.Helpers;

namespace AzureExtension.Controls.Forms;

public class SavePullRequestSearchForm : AzureForm<IPullRequestSearch>
{
    private readonly IResources _resources;

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
        { "{{PullRequestSearchDisplayNameLabel}}", _resources.GetResource("Forms_SavePullRequestSearch_TemplatePullRequestSearchDisplayNameLabel") },
        { "{{PullRequestSearchDisplayName}}", SavedSearch?.Name ?? string.Empty },
        { "{{PullRequestSearchDisplayNamePlaceholder}}", _resources.GetResource("Forms_SavePullRequestSearch_TemplatePullRequestSearchDisplayNamePlaceholder") },
        { "{{IsTopLevelTitle}}", _resources.GetResource("Forms_SavePullRequestSearch_TemplateIsTopLevelTitle") },
        { "{{IsTopLevel}}", IsTopLevelChecked },
        { "{{SavePullRequestSearchActionTitle}}", _resources.GetResource("Forms_SavePullRequestSearch_TemplateSavePullRequestSearchActionTitle") },
    };

    // for saving a new pull request search
    public SavePullRequestSearchForm(
        IResources resources,
        SavedAzureSearchesMediator mediator,
        IAccountProvider accountProvider,
        ISavedSearchesUpdater<IPullRequestSearch> pullRequestSearchRepository)
        : base(null, pullRequestSearchRepository, mediator, accountProvider)
    {
        _resources = resources;
        TemplateKey = "SavePullRequestSearch";
    }

    // for editing an existing pull request search
    public SavePullRequestSearchForm(
        IPullRequestSearch savedPullRequestSearch,
        IResources resources,
        SavedAzureSearchesMediator mediator,
        IAccountProvider accountProvider,
        ISavedSearchesUpdater<IPullRequestSearch> pullRequestSearchRepository)
        : base(savedPullRequestSearch, pullRequestSearchRepository, mediator, accountProvider)
    {
        _resources = resources;
        TemplateKey = "SavePullRequestSearch";
    }

    protected override IPullRequestSearch CreateSearchFromJson(JsonNode? jsonNode)
    {
        var enteredUrl = jsonNode?["url"]?.ToString() ?? string.Empty;
        var view = jsonNode?["view"]?.ToString() ?? string.Empty;
        var displayName = jsonNode?["PullRequestSearchDisplayName"]?.ToString() ?? string.Empty;
        var isTopLevel = jsonNode?["IsTopLevel"]?.ToString() == "true";

        var testUri = new AzureUri(enteredUrl);
        var url = CreatePullRequestUrl(testUri, view);
        var searchUri = new AzureUri(url);
        var name = string.IsNullOrWhiteSpace(displayName) ? string.Format(CultureInfo.CurrentCulture, "{0} - {1}", searchUri.Repository, view) : displayName;

        return new PullRequestSearchCandidate(searchUri, name, view, isTopLevel);
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
