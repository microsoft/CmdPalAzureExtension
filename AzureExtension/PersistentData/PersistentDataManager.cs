// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Client;
using AzureExtension.Controls;
using AzureExtension.DataModel;
using Microsoft.Identity.Client;
using Serilog;
using Windows.Storage;

namespace AzureExtension.PersistentData;

public class PersistentDataManager : IDisposable, IQueryRepository
{
    private static readonly Lazy<ILogger> _logger = new(() => Serilog.Log.ForContext("SourceContext", nameof(PersistentDataManager)));

    private static readonly ILogger _log = _logger.Value;

    private const string DataStoreFileName = "PersistentAzureData.db";

    private readonly IAzureValidator _azureValidator;

    private DataStore DataStore { get; set; }

    public DataStoreOptions DataStoreOpions { get; set; }

    private void ValidateDataStore()
    {
        if (DataStore == null || !DataStore.IsConnected)
        {
            throw new DataStoreInaccessibleException("DataStore is not available.");
        }
    }

    private static readonly Lazy<DataStoreOptions> _lazyDataStoreOptions = new(DefaultOptionsInit);

    private static DataStoreOptions DefaultOptions => _lazyDataStoreOptions.Value;

    private static DataStoreOptions DefaultOptionsInit()
    {
        return new DataStoreOptions
        {
            DataStoreFolderPath = ApplicationData.Current.LocalFolder.Path,
            DataStoreSchema = new PersistentDataSchema(),
        };
    }

    public PersistentDataManager(IAzureValidator azureValidator, DataStoreOptions? dataStoreOptions = null)
    {
        _azureValidator = azureValidator;

        dataStoreOptions ??= DefaultOptions;

        if (dataStoreOptions.DataStoreSchema is null)
        {
            throw new ArgumentNullException(nameof(dataStoreOptions), "DataStoreSchema cannot be null.");
        }

        DataStoreOpions = dataStoreOptions;

        DataStore = new DataStore(
            "PersistentDataStore",
            Path.Combine(dataStoreOptions.DataStoreFolderPath, DataStoreFileName),
            dataStoreOptions.DataStoreSchema);

        DataStore.Create(false);
    }

    private bool _disposed;

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            DataStore?.Dispose();
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public Task AddSavedQueryAsync(IQuery query)
    {
        ValidateDataStore();

        var name = query.Name;
        var url = query.Url;

        _log.Information($"Adding query: {name} - {url}.");
        if (Query.Get(DataStore, name, url) != null)
        {
            throw new InvalidOperationException($"Search {name} - {url} already exists.");
        }

        Query.Add(DataStore, name, url, false);

        return Task.CompletedTask;
    }

    public Task RemoveSavedQueryAsync(IQuery query)
    {
        ValidateDataStore();

        var name = query.Name;
        var url = query.Url;

        _log.Information($"Removing query: {name} - {url}.");
        if (Query.Get(DataStore, name, url) == null)
        {
            throw new InvalidOperationException($"Search {name} - {url} not found.");
        }

        Query.Remove(DataStore, name, url);

        return Task.CompletedTask;
    }

    private Task<IEnumerable<IQuery>> GetAllQueriesAsync(bool isTopLevel)
    {
        ValidateDataStore();
        if (isTopLevel)
        {
            return Task.FromResult(Query.GetTopLevel(DataStore));
        }

        return Task.FromResult(Query.GetAll(DataStore));
    }

    // ISearchRepository implementation
    public IQuery GetQuery(string name, string url)
    {
        ValidateDataStore();
        return Query.Get(DataStore, name, url) ?? throw new InvalidOperationException($"Search {name} - {url} not found.");
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
        var dsQuery = Query.Get(DataStore, query.Name, query.Url);
        return dsQuery != null ? Task.FromResult(dsQuery.IsTopLevel) : Task.FromResult(false);
    }

    public void UpdateQueryTopLevelStatus(IQuery query, bool isTopLevel, IAccount account)
    {
        ValidateQuery(query, account);
        ValidateDataStore();
        Query.AddOrUpdate(DataStore, query.Name, query.Url, isTopLevel);
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
                    Query.AddOrUpdate(DataStore, query.Name, query.Url, true);
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
}
