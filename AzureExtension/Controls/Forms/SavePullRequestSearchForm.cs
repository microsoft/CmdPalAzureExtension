// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Nodes;
using AzureExtension.Account;
using AzureExtension.Client;
using AzureExtension.Controls.Commands;
using AzureExtension.Helpers;

namespace AzureExtension.Controls.Forms;

public class SavePullRequestSearchForm : AzureForm<IPullRequestSearch>
{
    private readonly IResources _resources;

    private readonly IAccountProvider _accountProvider;

    private readonly AzureClientHelpers _azureClientHelpers;

    public bool IsEditing => SavedSearch != null;

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

    // for saving a new pull request search
    public SavePullRequestSearchForm(
        IResources resources,
        SavedAzureSearchesMediator mediator,
        IAccountProvider accountProvider,
        AzureClientHelpers azureClientHelpers,
        ISavedSearchesUpdater<IPullRequestSearch> pullRequestSearchRepository,
        SaveSearchCommand<IPullRequestSearch> saveSearchCommand)
        : base(null, pullRequestSearchRepository, mediator, accountProvider, saveSearchCommand)
    {
        _resources = resources;
        TemplateKey = "SavePullRequestSearch";
        _accountProvider = accountProvider;
        _azureClientHelpers = azureClientHelpers;
    }

    // for editing an existing pull request search
    public SavePullRequestSearchForm(
        IPullRequestSearch savedPullRequestSearch,
        IResources resources,
        SavedAzureSearchesMediator mediator,
        IAccountProvider accountProvider,
        AzureClientHelpers azureClientHelpers,
        ISavedSearchesUpdater<IPullRequestSearch> pullRequestSearchRepository,
        SaveSearchCommand<IPullRequestSearch> saveSearchCommand)
        : base(savedPullRequestSearch, pullRequestSearchRepository, mediator, accountProvider, saveSearchCommand)
    {
        _resources = resources;
        TemplateKey = "SavePullRequestSearch";
        _accountProvider = accountProvider;
        _azureClientHelpers = azureClientHelpers;
    }

    protected override IPullRequestSearch CreateSearchFromJson(JsonNode? jsonNode)
    {
        var enteredUrl = jsonNode?["url"]?.ToString() ?? string.Empty;
        var view = jsonNode?["view"]?.ToString() ?? string.Empty;
        var isTopLevel = jsonNode?["IsTopLevel"]?.ToString() == "true";

        var account = _accountProvider.GetDefaultAccount();
        var repoInfo = _azureClientHelpers.GetInfo(new AzureUri(enteredUrl), account, InfoType.Repository).Result;

        if (repoInfo.Result != ResultType.Success)
        {
            throw new InvalidOperationException($"Failed to get repository info {repoInfo.Error}: {repoInfo.ErrorMessage}");
        }

        var url = CreatePullRequestUrl(repoInfo.AzureUri, view);
        var name = $"{repoInfo.Name} - {view}";

        return new PullRequestSearchCandidate(repoInfo.AzureUri, name, view, isTopLevel);
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
