// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Account;
using AzureExtension.Client;
using AzureExtension.Controls;
using AzureExtension.Data;
using AzureExtension.DataModel;
using Build = AzureExtension.DataModel.Build;
using Definition = AzureExtension.DataModel.Definition;

namespace AzureExtension.DataManager;

public class AzureDataPipelineUpdater : IDataUpdater
{
    private readonly DataStore _dataStore;
    private readonly IAccountProvider _accountProvider;
    private readonly IAzureLiveDataProvider _liveDataProvider;
    private readonly IConnectionProvider _connectionProvider;
    private readonly ISavedSearchesSource<IPipelineDefinitionSearch> _definitionRepository;
    private readonly ISearchDataProvider<IPipelineDefinitionSearch, Definition> _pipelineProvider;

    public AzureDataPipelineUpdater(
        DataStore dataStore,
        IAccountProvider accountProvider,
        IAzureLiveDataProvider liveDataProvider,
        IConnectionProvider connectionProvider,
        ISavedSearchesSource<IPipelineDefinitionSearch> definitionRepository,
        ISearchDataProvider<IPipelineDefinitionSearch, Definition> pipelineProvider)
    {
        _dataStore = dataStore;
        _accountProvider = accountProvider;
        _liveDataProvider = liveDataProvider;
        _connectionProvider = connectionProvider;
        _definitionRepository = definitionRepository;
        _pipelineProvider = pipelineProvider;
    }

    public bool IsNewOrStale(IPipelineDefinitionSearch definitionSearch, TimeSpan refreshCooldown)
    {
        var dsDefinition = _pipelineProvider.GetDataForSearch(definitionSearch);
        return dsDefinition == null || DateTime.UtcNow - dsDefinition.UpdatedAt > refreshCooldown;
    }

    public bool IsNewOrStale(DataUpdateParameters parameters, TimeSpan refreshCooldown)
    {
        return IsNewOrStale((IPipelineDefinitionSearch)parameters.UpdateObject!, refreshCooldown);
    }

    public async Task UpdatePipelineAsync(IPipelineDefinitionSearch definitionSearch, CancellationToken cancellationToken)
    {
        var azureUri = new AzureUri(definitionSearch.ProjectUrl);
        var account = await _accountProvider.GetDefaultAccountAsync();
        var vssConnection = await _connectionProvider.GetVssConnectionAsync(azureUri.Uri, account);

        // Good practice to only create data after we know the client is valid, but any exceptions
        // will roll back the transaction.
        var org = Organization.GetOrCreate(_dataStore, azureUri.Connection);

        var project = Project.Get(_dataStore, azureUri.Project, org.Id);
        if (project is null)
        {
            var teamProject = await _liveDataProvider.GetTeamProject(vssConnection, azureUri.Project);
            project = Project.GetOrCreateByTeamProject(_dataStore, teamProject, org.Id);
        }

        var builds = await _liveDataProvider.GetBuildsAsync(vssConnection, project.InternalId, definitionSearch.InternalId, cancellationToken);

        foreach (var build in builds)
        {
            var dsDefinition = Definition.GetOrCreate(_dataStore, build.Definition, project.Id);
            var creator = Identity.GetOrCreateIdentity(_dataStore, build.RequestedBy, vssConnection, _liveDataProvider);
            var dsBuild = Build.GetOrCreate(_dataStore, build, dsDefinition.Id, creator.Id);
        }
    }

    private readonly TimeSpan _pipelineRetentionTime = TimeSpan.FromDays(7);

    public void PruneObsoleteData()
    {
        Build.DeleteBefore(_dataStore, DateTime.UtcNow - _pipelineRetentionTime);
        Definition.DeleteUnreferenced(_dataStore);
    }

    public async Task UpdateData(DataUpdateParameters parameters)
    {
        if (parameters.UpdateType == DataUpdateType.All)
        {
            var definitionSearches = _definitionRepository.GetSavedSearches();
            foreach (var definitionSearch in definitionSearches)
            {
                await UpdatePipelineAsync(definitionSearch, parameters.CancellationToken.GetValueOrDefault());
            }

            return;
        }

        await UpdatePipelineAsync((IPipelineDefinitionSearch)parameters.UpdateObject!, parameters.CancellationToken.GetValueOrDefault());
    }
}
