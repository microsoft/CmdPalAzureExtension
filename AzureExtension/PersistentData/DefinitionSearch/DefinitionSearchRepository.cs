// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls;
using AzureExtension.Data;
using AzureExtension.DataManager;
using Microsoft.Identity.Client;
using Serilog;

namespace AzureExtension.PersistentData;

public class DefinitionSearchRepository : ISavedSearchesProvider<IPipelineDefinitionSearch>, ISavedSearchesUpdater<IPipelineDefinitionSearch>, ISavedSearchesSource<IPipelineDefinitionSearch>
{
    private static readonly Lazy<ILogger> _logger = new(() => Log.ForContext("SourceContext", nameof(QueryRepository)));
    private static readonly ILogger _log = _logger.Value;

    private readonly IAzureValidator _azureValidator;
    private readonly DataStore _dataStore;

    public DefinitionSearchRepository(
        DataStore dataStore,
        IAzureValidator azureValidator)
    {
        _azureValidator = azureValidator;
        _dataStore = dataStore;
    }

    private void ValidateDataStore()
    {
        if (_dataStore == null || !_dataStore.IsConnected)
        {
            throw new DataStoreInaccessibleException("Peristent DataStore is not available.");
        }
    }

    private IEnumerable<IPipelineDefinitionSearch> GetAllDefinitionSearches(bool getTopLevelOnly)
    {
        ValidateDataStore();
        if (getTopLevelOnly)
        {
            return DefinitionSearch.GetTopLevel(_dataStore);
        }

        return DefinitionSearch.GetAll(_dataStore);
    }

    public bool IsTopLevel(IPipelineDefinitionSearch definitionSearch)
    {
        ValidateDataStore();
        var dsDefinitionSearch = DefinitionSearch.Get(_dataStore, definitionSearch.InternalId, definitionSearch.Url);
        return dsDefinitionSearch != null && dsDefinitionSearch.IsTopLevel;
    }

    public Task ValidateDefinitionSearch(IPipelineDefinitionSearch definitionSearch, IAccount account)
    {
        return _azureValidator.GetDefinitionInfo(definitionSearch.Url, definitionSearch.InternalId, account);
    }

    public Task Validate(IPipelineDefinitionSearch search, IAccount account)
    {
        return ValidateDefinitionSearch(search, account);
    }

    public void UpdateDefinitionSearchTopLevelStatus(IPipelineDefinitionSearch definitionSearch, bool isTopLevel, IAccount account)
    {
        ValidateDataStore();
        ValidateDefinitionSearch(definitionSearch, account).Wait();
        DefinitionSearch.AddOrUpdate(_dataStore, definitionSearch.Name, definitionSearch.InternalId, definitionSearch.Url, isTopLevel);
    }

    public void RemoveSavedSearch(IPipelineDefinitionSearch dataSearch)
    {
        ValidateDataStore();
        var internalId = dataSearch.InternalId;
        var url = dataSearch.Url;

        _log.Information($"Removing definition search: {internalId} - {url}.");
        if (DefinitionSearch.Get(_dataStore, internalId, url) == null)
        {
            throw new InvalidOperationException($"Definition search {internalId} - {url} not found.");
        }

        DefinitionSearch.Remove(_dataStore, internalId, url);
    }

    public void AddOrUpdateSearch(IPipelineDefinitionSearch dataSearch, bool isTopLevel)
    {
        _log.Information($"Adding or updating definition search: {dataSearch.InternalId} - {dataSearch.Url} - {dataSearch.Name!}.");
        ValidateDataStore();
        DefinitionSearch.AddOrUpdate(_dataStore, dataSearch.Name, dataSearch.InternalId, dataSearch.Url, isTopLevel);
    }

    public IEnumerable<IPipelineDefinitionSearch> GetSavedSearches()
    {
        ValidateDataStore();
        return GetAllDefinitionSearches(false);
    }

    public IEnumerable<IPipelineDefinitionSearch> GetSavedSearches(bool getTopLevelOnly = false)
    {
        ValidateDataStore();
        return GetAllDefinitionSearches(getTopLevelOnly);
    }
}
