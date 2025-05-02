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

public class AzureDataPipelineManager
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

    public IEnumerable<IDefinition> GetDefinitions(IPipelineSearch pipelineSearch)
    {
        var azureUri = new AzureUri(pipelineSearch.RepositoryUrl);

        var org = Organization.GetOrCreate(_dataStore, azureUri.Connection);
        if (org is null)
        {
            return Enumerable.Empty<Definition>();
        }

        var project = Project.Get(_dataStore, azureUri.Project, org.Id);
        if (project is null)
        {
            return Enumerable.Empty<Definition>();
        }

        return Definition.GetAll(_dataStore, project.Id);
    }

    public IEnumerable<IBuild> GetBuild(IDefinition definition)
    {
        var dsDefinition = Definition.GetByInternalId(_dataStore, definition.InternalId);
        if (dsDefinition is null)
        {
            return Enumerable.Empty<IBuild>();
        }

        return Build.GetForDefinition(_dataStore, dsDefinition.Id);
    }

    public async Task UpdatePipelineAsync(IPipelineSearch pipelineSearch, CancellationToken cancellationToken)
    {
        var azureUri = new AzureUri(pipelineSearch.RepositoryUrl);
        var account = await _accountProvider.GetDefaultAccountAsync();
        var vssConnection = await _connectionProvider.GetVssConnectionAsync(azureUri.Uri, account);

        var client = vssConnection.GetClient<BuildHttpClient>();

        // Good practice to only create data after we know the client is valid, but any exceptions
        // will roll back the transaction.
        var org = Organization.GetOrCreate(_dataStore, azureUri.Connection);

        var project = Project.Get(_dataStore, azureUri.Project, org.Id);
        if (project is null)
        {
            var teamProject = await _liveDataProvider.GetTeamProject(vssConnection, azureUri.Project);
            project = Project.GetOrCreateByTeamProject(_dataStore, teamProject, org.Id);
        }

        // Gets the last maximum of 1000 builds
        var builds = await client.GetBuildsAsync(project.InternalId, queryOrder: BuildQueryOrder.QueueTimeDescending, cancellationToken: cancellationToken);

        foreach (var build in builds)
        {
            var dsDefinition = Definition.GetOrCreate(_dataStore, build.Definition, project.Id);
            var creator = Identity.GetOrCreateIdentity(_dataStore, build.RequestedBy, vssConnection, _liveDataProvider);
            var dsBuild = Build.GetOrCreate(_dataStore, build, dsDefinition.Id, creator.Id);
        }
    }
}
