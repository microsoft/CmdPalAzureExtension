// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls;

namespace AzureExtension.PersistentData;

public partial class PersistentDataManager : ISavedPullRequestSearchRepository
{
    public Task AddSavedPullRequestSearch(IPullRequestSearch pullRequestSearch)
    {
        ValidateDataStore();

        var title = pullRequestSearch.Title;
        var url = pullRequestSearch.Url;
        var view = pullRequestSearch.View;

        _log.Information($"Adding pull request search: {title} - {url} - {view}.");
        if (PullRequestSearch.Get(_dataStore, title, url, view) != null)
        {
            _log.Error($"Pull request search {title} - {url} - {view} already exists.");
            return Task.CompletedTask;
        }

        PullRequestSearch.Add(_dataStore, title, url, view);

        return Task.CompletedTask;
    }

    public IPullRequestSearch GetPullRequestSearch(string title, string url, string view)
    {
        ValidateDataStore();
        return PullRequestSearch.Get(_dataStore, title, url, view) ?? throw new InvalidOperationException($"Pull request search {title} - {url} - {view} not found.");
    }

    public Task<IEnumerable<IPullRequestSearch>> GetAllPullRequestSearchesAsync(bool includeTopLevel)
    {
        ValidateDataStore();
        if (includeTopLevel)
        {
            return Task.FromResult(PullRequestSearch.GetTopLevel(_dataStore));
        }

        return Task.FromResult(PullRequestSearch.GetAll(_dataStore));
    }

    public Task<IEnumerable<IPullRequestSearch>> GetSavedPullRequestSearches()
    {
        return GetAllPullRequestSearchesAsync(false);
    }

    public Task<IEnumerable<IPullRequestSearch>> GetTopLevelPullRequestSearches()
    {
        return GetAllPullRequestSearchesAsync(true);
    }

    public Task<bool> IsTopLevel(IPullRequestSearch pullRequestSearch)
    {
        ValidateDataStore();
        var dstorePullRequestSearch = PullRequestSearch.Get(_dataStore, pullRequestSearch.Title, pullRequestSearch.Url, pullRequestSearch.View);
        return dstorePullRequestSearch != null ? Task.FromResult(dstorePullRequestSearch.IsTopLevel) : Task.FromResult(false);
    }

    public Task RemoveSavedPullRequestSearch(IPullRequestSearch pullRequestSearch)
    {
        ValidateDataStore();

        var title = pullRequestSearch.Title;
        var url = pullRequestSearch.Url;
        var view = pullRequestSearch.View;

        _log.Information($"Removing pull request search: {title} - {url} - {view}.");
        if (PullRequestSearch.Get(_dataStore, title, url, view) == null)
        {
            throw new InvalidOperationException($"Pull request search {title} - {url} - {view} not found.");
        }

        PullRequestSearch.Remove(_dataStore, title, url, view);
        return Task.CompletedTask;
    }

    public void UpdatePullRequestSearchTopLevelStatus(IPullRequestSearch pullRequestSearch, bool isTopLevel)
    {
        ValidateDataStore();
        PullRequestSearch.AddOrUpdate(_dataStore, pullRequestSearch.Url, pullRequestSearch.Title, pullRequestSearch.View, isTopLevel);
    }
}
