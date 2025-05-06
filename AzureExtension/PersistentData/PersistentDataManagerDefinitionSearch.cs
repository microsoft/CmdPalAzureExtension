// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Client;
using AzureExtension.Controls;
using AzureExtension.Data;
using Microsoft.Identity.Client;
using Microsoft.TeamFoundation.Build.WebApi;
using Serilog;

namespace AzureExtension.PersistentData;

public class PersistentDataManagerDefinitionSearch : IDefinitionRepository
{
    private static readonly Lazy<ILogger> _logger = new(() => Log.ForContext("SourceContext", nameof(PersistentDataManager)));
    private static readonly ILogger _log = _logger.Value;

    private readonly IAzureValidator _azureValidator;
    private readonly DataStore _dataStore;
    private readonly IAzureLiveDataProvider _liveDataProvider;
    private readonly IConnectionProvider _connectionProvider;

    public PersistentDataManagerDefinitionSearch(
        DataStore dataStore,
        IAzureValidator azureValidator,
        IAzureLiveDataProvider liveDataProvider,
        IConnectionProvider connectionProvider)
    {
        _azureValidator = azureValidator;
        _dataStore = dataStore;
        _liveDataProvider = liveDataProvider;
        _connectionProvider = connectionProvider;
    }

    private void ValidateDataStore()
    {
        if (_dataStore == null || !_dataStore.IsConnected)
        {
            throw new DataStoreInaccessibleException("Peristent DataStore is not available.");
        }
    }

    public Task AddSavedDefinitionSearch(IDefinitionSearch definitionSearch)
    {
        ValidateDataStore();
        var internalId = definitionSearch.InternalId;
        var projectUrl = definitionSearch.ProjectUrl;

        _log.Information($"Adding definition search: {internalId} - {projectUrl}.");
        if (DefinitionSearch.Get(_dataStore, internalId, projectUrl) != null)
        {
            _log.Error($"Definition search {internalId} - {projectUrl} already exists.");
            return Task.CompletedTask;
        }

        DefinitionSearch.Add(_dataStore, internalId, projectUrl);
        return Task.CompletedTask;
    }

    public IDefinitionSearch GetDefinitionSearch(long internalId, string projectUrl)
    {
        ValidateDataStore();
        return DefinitionSearch.Get(_dataStore, internalId, projectUrl) ?? throw new InvalidOperationException($"Definition search {internalId} - {projectUrl} not found.");
    }

    public async Task<IDefinition> GetDefinition(IDefinitionSearch definitionSearch, IAccount account)
    {
        var azureUri = new AzureUri(definitionSearch.ProjectUrl);
        var vssConnection = await _connectionProvider.GetVssConnectionAsync(azureUri.Connection, account);
        var definitionBuild = await _liveDataProvider.GetDefinitionAsync(vssConnection, azureUri.Project, definitionSearch.InternalId, CancellationToken.None);
        return new Definition { InternalId = definitionBuild.Id, Name = definitionBuild.Name };
    }

    public async Task<IEnumerable<IDefinition>> GetAllDefinitionsAsync(bool includeTopLevel, IAccount account)
    {
        ValidateDataStore();
        var definitions = new List<IDefinition>();
        var definitionSearches = await GetAllDefinitionSearchesAsync(includeTopLevel);

        // This for is needed because there can be different projects.
        // If this need to be sped up, we can group by project and run them in parallel.
        // For each project, we could use one single API call to ADO.
        foreach (var definitionSearch in definitionSearches)
        {
            var definition = await GetDefinition(definitionSearch, account);
            definitions.Add(definition);
        }

        return definitions;
    }

    public Task<IEnumerable<IDefinitionSearch>> GetAllDefinitionSearchesAsync(bool includeTopLevel)
    {
        ValidateDataStore();
        if (includeTopLevel)
        {
            return Task.FromResult(DefinitionSearch.GetTopLevel(_dataStore));
        }

        return Task.FromResult(DefinitionSearch.GetAll(_dataStore));
    }

    public Task<IEnumerable<IDefinitionSearch>> GetSavedDefinitionSearches()
    {
        return GetAllDefinitionSearchesAsync(false);
    }

    public Task<IEnumerable<IDefinitionSearch>> GetTopLevelDefinitionSearches()
    {
        return GetAllDefinitionSearchesAsync(true);
    }

    public Task<bool> IsTopLevel(IDefinitionSearch definitionSearch)
    {
        ValidateDataStore();
        var dsDefinitionSearch = DefinitionSearch.Get(_dataStore, definitionSearch.InternalId, definitionSearch.ProjectUrl);
        return Task.FromResult(dsDefinitionSearch != null && dsDefinitionSearch.IsTopLevel);
    }

    public Task RemoveSavedDefinitionSearch(IDefinitionSearch definitionSearch)
    {
        ValidateDataStore();
        var internalId = definitionSearch.InternalId;
        var projectUrl = definitionSearch.ProjectUrl;

        _log.Information($"Removing definition search: {internalId} - {projectUrl}.");
        if (DefinitionSearch.Get(_dataStore, internalId, projectUrl) == null)
        {
            _log.Error($"Definition search {internalId} - {projectUrl} not found.");
            return Task.CompletedTask;
        }

        DefinitionSearch.Remove(_dataStore, internalId, projectUrl);
        return Task.CompletedTask;
    }

    public string ValidateDefinitionSearch(IDefinitionSearch definitionSearch, IAccount account)
    {
        var definitionInfo = _azureValidator.GetDefinitionInfo(definitionSearch.ProjectUrl, definitionSearch.InternalId, account);
        return definitionInfo.Name;
    }

    public void UpdateDefinitionSearchTopLevelStatus(IDefinitionSearch definitionSearch, bool isTopLevel, IAccount account)
    {
        ValidateDataStore();
        ValidateDefinitionSearch(definitionSearch, account);
        DefinitionSearch.AddOrUpdate(_dataStore, definitionSearch.InternalId, definitionSearch.ProjectUrl, isTopLevel);
    }
}
