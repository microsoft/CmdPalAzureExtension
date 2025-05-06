// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Client;
using AzureExtension.Controls;
using AzureExtension.Data;
using Microsoft.Identity.Client;
using Serilog;

namespace AzureExtension.PersistentData;

public partial class PersistentDataManager : IQueryRepository
{
    private static readonly Lazy<ILogger> _logger = new(() => Log.ForContext("SourceContext", nameof(PersistentDataManager)));

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

    public PersistentDataManager(DataStore dataStore, IAzureValidator azureValidator)
    {
        _azureValidator = azureValidator;
        _dataStore = dataStore;
    }

    public Task AddSavedQueryAsync(IQuery query)
    {
        ValidateDataStore();

        var name = query.Name;
        var url = query.Url;

        _log.Information($"Adding query: {name} - {url}.");
        if (Query.Get(_dataStore, name, url) != null)
        {
            throw new InvalidOperationException($"Search {name} - {url} already exists.");
        }

        Query.Add(_dataStore, name, url, false);

        return Task.CompletedTask;
    }

    public Task RemoveSavedQueryAsync(IQuery query)
    {
        ValidateDataStore();

        var name = query.Name;
        var url = query.Url;

        _log.Information($"Removing query: {name} - {url}.");
        if (Query.Get(_dataStore, name, url) == null)
        {
            throw new InvalidOperationException($"Search {name} - {url} not found.");
        }

        Query.Remove(_dataStore, name, url);
        return Task.CompletedTask;
    }

    private Task<IEnumerable<IQuery>> GetAllQueriesAsync(bool isTopLevel)
    {
        ValidateDataStore();
        if (isTopLevel)
        {
            return Task.FromResult(Query.GetTopLevel(_dataStore));
        }

        return Task.FromResult(Query.GetAll(_dataStore));
    }

    // ISearchRepository implementation
    public IQuery GetQuery(string name, string url)
    {
        ValidateDataStore();
        return Query.Get(_dataStore, name, url) ?? throw new InvalidOperationException($"Search {name} - {url} not found.");
    }

    public Task<IEnumerable<IQuery>> GetSavedQueries()
    {
        return GetAllQueriesAsync(false);
    }

    public Task<IEnumerable<IQuery>> GetTopLevelQueries()
    {
        return GetAllQueriesAsync(true);
    }

    public Task<bool> IsTopLevel(IQuery query)
    {
        ValidateDataStore();
        var dsQuery = Query.Get(_dataStore, query.Name, query.Url);
        return dsQuery != null ? Task.FromResult(dsQuery.IsTopLevel) : Task.FromResult(false);
    }

    public void UpdateQueryTopLevelStatus(IQuery query, bool isTopLevel, IAccount account)
    {
        ValidateQuery(query, account);
        ValidateDataStore();
        Query.AddOrUpdate(_dataStore, query.Name, query.Url, isTopLevel);
    }

    private readonly object _insertLock = new();

    public async Task InitializeTopLevelQueries(IEnumerable<IQuery> queries, IAccount account)
    {
        var defaultTasks = new List<Task>();
        foreach (var query in queries)
        {
            var task = Task.Run(() =>
            {
                _log.Information($"Validating search: {query.Name} - {query.Url}.");

                if (!ValidateQuery(query, account))
                {
                    _log.Error($"Search {query.Name}  -  {query.Url} is invalid.");
                    return;
                }

                // We can't have multiple threads inserting into the database at the same time.
                // But doing that asynchronously will not keep the original order of the searches.
                lock (_insertLock)
                {
                    _log.Information($"Adding search: {query.Name}  -  {query.Url}.");
                    ValidateDataStore();
                    Query.AddOrUpdate(_dataStore, query.Name, query.Url, true);
                }
            });

            defaultTasks.Add(task);
        }

        await Task.WhenAll(defaultTasks);
    }

    public bool ValidateQuery(IQuery query, IAccount account)
    {
        var queryInfo = _azureValidator.GetQueryInfo(query.Url, account);
        return queryInfo.Result == ResultType.Success;
    }

    public Task Remove(IAzureSearch azureSearch)
    {
        if (azureSearch is IQuery)
        {
            return RemoveSavedQueryAsync((IQuery)azureSearch);
        }
        else if (azureSearch is IPullRequestSearch)
        {
            return RemoveSavedPullRequestSearch((IPullRequestSearch)azureSearch);
        }

        throw new InvalidOperationException($"Unknown search type: {azureSearch.GetType()}");
    }
}
