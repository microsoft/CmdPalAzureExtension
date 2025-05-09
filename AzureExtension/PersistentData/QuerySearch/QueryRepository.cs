// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Client;
using AzureExtension.Controls;
using AzureExtension.Data;
using Microsoft.Identity.Client;
using Serilog;

namespace AzureExtension.PersistentData;

public partial class QueryRepository : IPersistentSearchRepository<IQuery>
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

    public QueryRepository(DataStore dataStore, IAzureValidator azureValidator)
    {
        _azureValidator = azureValidator;
        _dataStore = dataStore;
    }

    public bool IsTopLevel(IQuery dataSearch)
    {
        ValidateDataStore();
        var dsQuery = Query.Get(_dataStore, dataSearch.Name, dataSearch.Url);
        return dsQuery != null && dsQuery.IsTopLevel;
    }

    public async Task<bool> ValidateQuery(IQuery query, IAccount account)
    {
        var queryInfo = await _azureValidator.GetQueryInfo(query.Url, account);
        return queryInfo.Result == ResultType.Success;
    }

    public void RemoveSavedSearch(IQuery dataSearch)
    {
        ValidateDataStore();

        var name = dataSearch.Name;
        var url = dataSearch.Url;

        _log.Information($"Removing query: {name} - {url}.");
        if (Query.Get(_dataStore, name, url) == null)
        {
            throw new InvalidOperationException($"Search {name} - {url} not found.");
        }

        Query.Remove(_dataStore, name, url);
    }

    public IQuery GetSavedData(IQuery dataSearch)
    {
        ValidateDataStore();

        var name = dataSearch.Name;
        var url = dataSearch.Url;

        return Query.Get(_dataStore, name, url) ?? throw new InvalidOperationException($"Search {name} - {url} not found.");
    }

    public IEnumerable<IQuery> GetSavedSearches(bool getTopLevelOnly = false)
    {
        ValidateDataStore();
        if (getTopLevelOnly)
        {
            return Query.GetTopLevel(_dataStore);
        }

        return Query.GetAll(_dataStore);
    }

    public async Task AddOrUpdateSearch(IQuery dataSearch, bool isTopLevel, IAccount account)
    {
        ValidateDataStore();
        await ValidateQuery(dataSearch, account);
        Query.AddOrUpdate(_dataStore, dataSearch.Name, dataSearch.Url, isTopLevel);
    }

    public IEnumerable<IQuery> GetAllSavedData(bool getTopLevelOnly = false)
    {
        return GetSavedSearches(getTopLevelOnly);
    }
}
