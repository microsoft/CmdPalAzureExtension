// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Account;
using AzureExtension.Client;
using AzureExtension.Controls;
using AzureExtension.Data;
using AzureExtension.DataModel;
using Microsoft.TeamFoundation.Build.WebApi;
using Build = AzureExtension.DataModel.Build;

namespace AzureExtension.DataManager;

public class AzureDataPipelineManager : IPipelineProvider, IPipelineUpdater, IDataUpdater
{
    private readonly DataStore _dataStore;
    private readonly IAccountProvider _accountProvider;
    private readonly IAzureLiveDataProvider _liveDataProvider;
    private readonly IConnectionProvider _connectionProvider;

    public AzureDataPipelineManager(DataStore dataStore, IAccountProvider accountProvider, IAzureLiveDataProvider liveDataProvider, IConnectionProvider connectionProvider)
    {
        _dataStore = dataStore;
        _accountProvider = accountProvider;
        _liveDataProvider = liveDataProvider;
        _connectionProvider = connectionProvider;
    }

    public Definition? GetDefinition(IDefinitionSearch definitionSearch)
    {
         return Definition.GetByInternalId(_dataStore, definitionSearch.InternalId);
    }

    public IEnumerable<IBuild> GetBuilds(IDefinitionSearch definitionSearch)
    {
        var dsDefinition = GetDefinition(definitionSearch);
        if (dsDefinition is null)
        {
            return Enumerable.Empty<IBuild>();
        }

        return Build.GetForDefinition(_dataStore, dsDefinition.Id);
    }

    public bool IsNewOrStale(IDefinitionSearch definitionSearch, TimeSpan refreshCooldown)
    {
        var dsDefinition = GetDefinition(definitionSearch);
        return dsDefinition == null || DateTime.UtcNow - dsDefinition.UpdatedAt > refreshCooldown;
    }

    public bool IsNewOrStale(DataUpdateParameters parameters, TimeSpan refreshCooldown)
    {
        return IsNewOrStale((IDefinitionSearch)parameters.UpdateObject!, refreshCooldown);
    }

    public async Task UpdatePipelineAsync(IDefinitionSearch definitionSearch, CancellationToken cancellationToken)
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

    public Task UpdateData(DataUpdateParameters parameters)
    {
        return UpdatePipelineAsync((IDefinitionSearch)parameters.UpdateObject!, parameters.CancellationToken.GetValueOrDefault());
    }
}
