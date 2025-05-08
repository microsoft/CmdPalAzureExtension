// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Account;
using AzureExtension.Client;
using AzureExtension.Controls;
using AzureExtension.Data;
using AzureExtension.DataManager;
using Microsoft.Identity.Client;
using Serilog;

namespace AzureExtension.PersistentData;

public class DefinitionSearchRepository : IPersistentSearchRepository<IPipelineDefinitionSearch, IDefinition>, ISavedSearchesSource<IPipelineDefinitionSearch>
{
    private static readonly Lazy<ILogger> _logger = new(() => Log.ForContext("SourceContext", nameof(QueryRepository)));
    private static readonly ILogger _log = _logger.Value;

    private readonly IAzureValidator _azureValidator;
    private readonly DataStore _dataStore;
    private readonly IAzureLiveDataProvider _liveDataProvider;
    private readonly IConnectionProvider _connectionProvider;
    private readonly IDefinitionProvider _pipelineProvider;
    private readonly IAccountProvider _accountProvider;

    public DefinitionSearchRepository(
        DataStore dataStore,
        IAzureValidator azureValidator,
        IAzureLiveDataProvider liveDataProvider,
        IConnectionProvider connectionProvider,
        IDefinitionProvider pipelineProvider,
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

    public IDefinition GetSavedData(IPipelineDefinitionSearch dataSearch)
    {
        var dsDefinition = _pipelineProvider.GetDefinition(dataSearch);

        if (dsDefinition != null)
        {
            dsDefinition.HtmlUrl = dataSearch.ProjectUrl;
            return dsDefinition;
        }

        return Task.Run(async () =>
        {
            var account = await _accountProvider.GetDefaultAccountAsync();
            var azureUri = new AzureUri(dataSearch.ProjectUrl);
            var vssConnection = await _connectionProvider.GetVssConnectionAsync(azureUri.Connection, account);
            var definitionBuild = await _liveDataProvider.GetDefinitionAsync(vssConnection, azureUri.Project, dataSearch.InternalId, CancellationToken.None);
            return new Definition { InternalId = definitionBuild.Id, Name = definitionBuild.Name, HtmlUrl = dataSearch.ProjectUrl };
        }).Result;
    }

    public IEnumerable<IDefinition> GetAllSavedData(bool getTopLevelOnly = false)
    {
        ValidateDataStore();
        var definitions = new List<IDefinition>();
        var definitionSearches = GetAllDefinitionSearches(getTopLevelOnly);

        // This for is needed because there can be different projects.
        // If this need to be sped up, we can group by project and run them in parallel.
        // For each project, we could use one single API call to ADO.
        foreach (var definitionSearch in definitionSearches)
        {
            var definition = GetSavedData(definitionSearch);
            definitions.Add(definition);
        }

        return definitions;
    }

    public async Task AddOrUpdateSearch(IPipelineDefinitionSearch dataSearch, bool isTopLevel, IAccount account)
    {
        ValidateDataStore();
        await ValidateDefinitionSearch(dataSearch, account);
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
