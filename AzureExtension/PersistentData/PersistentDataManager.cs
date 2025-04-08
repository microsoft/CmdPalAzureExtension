// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Client;
using AzureExtension.Controls;
using AzureExtension.DataModel;
using AzureExtension.DeveloperId;
using Serilog;
using Windows.Devices;
using Windows.Storage;

namespace AzureExtension.PersistentData;

public class PersistentDataManager : IDisposable, ISearchRepository
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

    public async Task<IEnumerable<Repository>> GetAllRepositoriesAsync()
    {
        return await Task.Run(() =>
        {
            ValidateDataStore();
            return Repository.GetAll(DataStore);
        });
    }

    private Task AddSearchAsync(ISearch search)
    {
        return Task.Run(() =>
        {
            ValidateDataStore();

            var name = search.Name;
            var searchString = search.SearchString;

            _log.Information($"Adding search: {name} - {searchString}.");
            if (Search.Get(DataStore, name, searchString) != null)
            {
                throw new InvalidOperationException($"Search {name} - {searchString} already exists.");
            }

            Search.Add(DataStore, name, searchString);
        });
    }

    private async Task RemoveSearchAsync(string name, string searchString)
    {
        await Task.Run(() =>
        {
            ValidateDataStore();
            _log.Information($"Removing search: {name} - {searchString}.");
            Search.Remove(DataStore, name, searchString);
        });
    }

    private async Task<IEnumerable<ISearch>> GetAllSearchesAsync(bool isTopLevel)
    {
        return await Task.Run(() =>
        {
            ValidateDataStore();
            if (isTopLevel)
            {
                return Search.GetAllTopLevel(DataStore);
            }

            return Search.GetAll(DataStore);
        });
    }

    // ISearchRepository implementation
    public ISearch GetSearch(string name, string searchString)
    {
        ValidateDataStore();
        return Search.Get(DataStore, name, searchString) ?? throw new InvalidOperationException($"Search {name} - {searchString} not found.");
    }

    public Task<IEnumerable<ISearch>> GetSavedSearches()
    {
        return GetAllSearchesAsync(false);
    }

    public Task<IEnumerable<ISearch>> GetTopLevelSearches()
    {
        return GetAllSearchesAsync(true);
    }

    public Task<bool> IsTopLevel(ISearch search)
    {
        ValidateDataStore();
        var dsSearch = Search.Get(DataStore, search.Name, search.SearchString);
        return dsSearch != null ? Task.FromResult(dsSearch.IsTopLevel) : Task.FromResult(false);
    }

    public void UpdateSearchTopLevelStatus(ISearch search, bool isTopLevel, IDeveloperId developerId)
    {
        ValidateSearch(search, developerId);
        ValidateDataStore();
        Search.AddOrUpdate(DataStore, search.Name, search.SearchString, isTopLevel);
    }

    public Task RemoveSavedSearch(ISearch search)
    {
        return RemoveSearchAsync(search.Name, search.SearchString);
    }

    public InfoResult GetQueryInfo(string queryUrl, string name, IDeveloperId developerId)
    {
        var queryInfo = _azureValidator.GetQueryInfo(queryUrl, name, developerId);
        return queryInfo;
    }

    public async Task AddSavedSearch(ISearch search)
    {
        // TODO: Add back validation
        await AddSearchAsync(search);
    }

    private readonly object _insertLock = new();

    public async Task InitializeTopLevelSearches(IEnumerable<ISearch> searches, IDeveloperId developerId)
    {
        var defaultTasks = new List<Task>();
        foreach (var search in searches)
        {
            var task = Task.Run(() =>
            {
                _log.Information($"Validating search: {search.Name} - {search.SearchString}.");
                var queryInfo = GetQueryInfo(search.SearchString, search.Name, developerId);

                if (queryInfo == null)
                {
                    _log.Error($"Search {search.Name} - {search.SearchString} is invalid.");
                    return;
                }

                // We can't have multiple threads inserting into the database at the same time.
                // But doing that asynchronously will not keep the original order of the searches.
                lock (_insertLock)
                {
                    _log.Information($"Adding search: {search.Name} - {search.SearchString}.");
                    ValidateDataStore();
                    Search.AddOrUpdate(DataStore, search.Name, search.SearchString, true);
                }
            });

            defaultTasks.Add(task);
        }

        await Task.WhenAll(defaultTasks);
    }

    public bool ValidateSearch(ISearch search, IDeveloperId developerId)
    {
        var queryInfo = GetQueryInfo(search.SearchString, search.Name, developerId);
        return queryInfo.Result == ResultType.Success;
    }

    public Task InitializeTopLevelSearches(IEnumerable<ISearch> searches)
    {
        throw new NotImplementedException();
    }
}
