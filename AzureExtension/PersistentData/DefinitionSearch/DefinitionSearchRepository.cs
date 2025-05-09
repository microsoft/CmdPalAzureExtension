// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Account;
using AzureExtension.Client;
using AzureExtension.Controls;
using AzureExtension.Data;
using AzureExtension.DataManager;
using AzureExtension.DataModel;
using Microsoft.Identity.Client;
using Serilog;

namespace AzureExtension.PersistentData;

public class DefinitionSearchRepository : ISavedSearchesProvider<IPipelineDefinitionSearch>, ISavedSearchesUpdater<IPipelineDefinitionSearch>, ISavedSearchesSource<IPipelineDefinitionSearch>
{
    private static readonly Lazy<ILogger> _logger = new(() => Log.ForContext("SourceContext", nameof(QueryRepository)));
    private static readonly ILogger _log = _logger.Value;

    private readonly IAzureValidator _azureValidator;
    private readonly DataStore _dataStore;
    private readonly IAzureLiveDataProvider _liveDataProvider;
    private readonly IConnectionProvider _connectionProvider;
    private readonly IDataProvider<IPipelineDefinitionSearch, DataModel.Definition, Build> _pipelineProvider;
    private readonly IAccountProvider _accountProvider;

    public DefinitionSearchRepository(
        DataStore dataStore,
        IAzureValidator azureValidator,
        IAzureLiveDataProvider liveDataProvider,
        IConnectionProvider connectionProvider,
        IDataProvider<IPipelineDefinitionSearch, DataModel.Definition, Build> pipelineProvider,
        IAccountProvider accountProvider)
    {
        _azureValidator = azureValidator;
        _dataStore = dataStore;
        _liveDataProvider = liveDataProvider;
        _connectionProvider = connectionProvider;
        _pipelineProvider = pipelineProvider;
        _accountProvider = accountProvider;
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
            DefinitionSearch.GetTopLevel(_dataStore);
        }

        return DefinitionSearch.GetAll(_dataStore);
    }

    public bool IsTopLevel(IPipelineDefinitionSearch definitionSearch)
    {
        ValidateDataStore();
        var dsDefinitionSearch = DefinitionSearch.Get(_dataStore, definitionSearch.InternalId, definitionSearch.ProjectUrl);
        return dsDefinitionSearch != null && dsDefinitionSearch.IsTopLevel;
    }

    public Task ValidateDefinitionSearch(IPipelineDefinitionSearch definitionSearch, IAccount account)
    {
        return _azureValidator.GetDefinitionInfo(definitionSearch.ProjectUrl, definitionSearch.InternalId, account);
    }

    public Task Validate(IPipelineDefinitionSearch search, IAccount account)
    {
        return ValidateDefinitionSearch(search, account);
    }

    public void UpdateDefinitionSearchTopLevelStatus(IPipelineDefinitionSearch definitionSearch, bool isTopLevel, IAccount account)
    {
        ValidateDataStore();
        ValidateDefinitionSearch(definitionSearch, account).Wait();
        DefinitionSearch.AddOrUpdate(_dataStore, definitionSearch.InternalId, definitionSearch.ProjectUrl, isTopLevel);
    }

    public void RemoveSavedSearch(IPipelineDefinitionSearch dataSearch)
    {
        ValidateDataStore();
        var internalId = dataSearch.InternalId;
        var projectUrl = dataSearch.ProjectUrl;

        _log.Information($"Removing definition search: {internalId} - {projectUrl}.");
        if (DefinitionSearch.Get(_dataStore, internalId, projectUrl) == null)
        {
            throw new InvalidOperationException($"Definition search {internalId} - {projectUrl} not found.");
        }

        DefinitionSearch.Remove(_dataStore, internalId, projectUrl);
    }

    public void AddOrUpdateSearch(IPipelineDefinitionSearch dataSearch, bool isTopLevel)
    {
        ValidateDataStore();
        DefinitionSearch.AddOrUpdate(_dataStore, dataSearch.InternalId, dataSearch.ProjectUrl, isTopLevel);
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
