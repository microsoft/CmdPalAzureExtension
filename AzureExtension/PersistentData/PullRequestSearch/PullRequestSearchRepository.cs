// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls;
using AzureExtension.Data;
using Microsoft.Identity.Client;
using Serilog;

namespace AzureExtension.PersistentData;

public class PullRequestSearchRepository : IPersistentSearchRepository<IPullRequestSearch>
{
    private static readonly Lazy<ILogger> _logger = new(() => Log.ForContext("SourceContext", nameof(QueryRepository)));

    private static readonly ILogger _log = _logger.Value;

    private readonly IAzureValidator _azureValidator;
    private readonly DataStore _dataStore;

    private void ValidateDataStore()
    {
        if (_dataStore == null || !_dataStore.IsConnected)
        {
            throw new DataStoreInaccessibleException("Peristent DataStore is not available.");
        }
    }

    public PullRequestSearchRepository(DataStore dataStore, IAzureValidator azureValidator)
    {
        _azureValidator = azureValidator;
        _dataStore = dataStore;
    }

    private Task ValidatePullRequestSearch(IPullRequestSearch pullRequestSearch, IAccount account)
    {
        return _azureValidator.GetRepositoryInfo(pullRequestSearch.Url, account);
    }

    public Task Validate(IPullRequestSearch search, IAccount account)
    {
        return ValidatePullRequestSearch(search, account);
    }

    public void RemoveSavedSearch(IPullRequestSearch dataSearch)
    {
        ValidateDataStore();

        var title = dataSearch.Name;
        var url = dataSearch.Url;
        var view = dataSearch.View;

        _log.Information($"Removing pull request search: {title} - {url} - {view}.");
        if (PullRequestSearch.Get(_dataStore, url, title, view) == null)
        {
            throw new InvalidOperationException($"Pull request search {title} - {url} - {view} not found.");
        }

        PullRequestSearch.Remove(_dataStore, url, title, view);
    }

    public IPullRequestSearch GetSavedData(IPullRequestSearch dataSearch)
    {
        ValidateDataStore();

        var title = dataSearch.Name;
        var url = dataSearch.Url;
        var view = dataSearch.View;

        return PullRequestSearch.Get(_dataStore, url, title, view) ?? throw new InvalidOperationException($"Pull request search {title} - {url} - {view} not found.");
    }

    public IEnumerable<IPullRequestSearch> GetSavedSearches(bool getTopLevelOnly = false)
    {
        ValidateDataStore();
        if (getTopLevelOnly)
        {
            return PullRequestSearch.GetTopLevel(_dataStore);
        }

        return PullRequestSearch.GetAll(_dataStore);
    }

    public bool IsTopLevel(IPullRequestSearch dataSearch)
    {
        ValidateDataStore();
        var dstorePullRequestSearch = PullRequestSearch.Get(_dataStore, dataSearch.Url, dataSearch.Name, dataSearch.View);
        return dstorePullRequestSearch != null && dstorePullRequestSearch.IsTopLevel;
    }

    public void AddOrUpdateSearch(IPullRequestSearch dataSearch, bool isTopLevel)
    {
        ValidateDataStore();
        PullRequestSearch.AddOrUpdate(_dataStore, dataSearch.Url, dataSearch.Name, dataSearch.View, isTopLevel);
    }

    public IEnumerable<IPullRequestSearch> GetAllSavedData(bool getTopLevelOnly = false)
    {
        return GetSavedSearches(getTopLevelOnly);
    }
}
