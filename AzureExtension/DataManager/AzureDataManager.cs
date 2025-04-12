// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Account;
using AzureExtension.Client;
using AzureExtension.Controls;
using AzureExtension.Data;
using AzureExtension.DataModel;
using AzureExtension.Helpers;
using Microsoft.Identity.Client;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using Serilog;
using TFModels = Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace AzureExtension.DataManager;

public class AzureDataManager
{
    private readonly ILogger _log;
    private readonly DataStore _dataStore;
    private readonly IAccountProvider _accountProvider;
    private readonly AzureClientProvider _azureClientProvider;

    public AzureDataManager(
        DataStore dataStore,
        IAccountProvider accountProvider,
        AzureClientProvider azureClientProvider)
    {
        _log = Log.ForContext("SourceContext", nameof(IDataProvider));
        _dataStore = dataStore;
        _accountProvider = accountProvider;
        _azureClientProvider = azureClientProvider;
    }

    private void ValidateDataStore()
    {
        if (_dataStore == null || !_dataStore.IsConnected)
        {
            throw new DataStoreInaccessibleException("Cache DataStore is not available.");
        }
    }

    public Identity GetIdentity(IdentityRef identityRef, VssConnection connection)
    {
        ValidateDataStore();
        return Identity.GetOrCreateIdentity(_dataStore, identityRef, connection);
    }

    public WorkItem? GetWorkItem(long workItemId, VssConnection connection, Project project)
    {
        ValidateDataStore();
        return WorkItem.Get(_dataStore, workItemId);
    }

    public async Task<IEnumerable<WorkItem>> UpdateWorkItems(IQuery query)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew(); // Start measuring time

        var azureUri = new AzureUri(query.Url);
        var account = _accountProvider.GetDefaultAccount();
        var result = _azureClientProvider.GetVssConnection(azureUri.Connection, account);

        if (result.Result != ResultType.Success)
        {
            if (result.Exception != null)
            {
                throw result.Exception;
            }
            else
            {
                throw new AzureAuthorizationException($"Failed getting connection: {azureUri.Connection} with {result.Error}");
            }
        }

        var witClient = result.Connection!.GetClient<WorkItemTrackingHttpClient>();
        if (witClient == null)
        {
            throw new AzureClientException($"Failed getting WorkItemTrackingHttpClient");
        }

        // Good practice to only create data after we know the client is valid, but any exceptions
        // will roll back the transaction.
        var org = Organization.GetOrCreate(_dataStore, azureUri.Connection);

        if (org == null)
        {
            throw new AzureClientException($"Failed getting Organization for {azureUri.Connection}");
        }

        var project = Project.Get(_dataStore, azureUri.Project, org.Id);
        if (project is null)
        {
            var teamProject = await GetTeamProject(azureUri.Project, account, azureUri.Connection);
            project = Project.GetOrCreateByTeamProject(_dataStore, teamProject, org.Id);
        }

        var getQueryResult = await witClient.GetQueryAsync(project.InternalId, azureUri.Query);
        if (getQueryResult == null)
        {
            throw new AzureClientException($"GetQueryAsync failed for {azureUri.Connection}, {project.InternalId}, {azureUri.Query}");
        }

        var queryId = new Guid(azureUri.Query);
        var queryResult = await witClient.QueryByIdAsync(project.InternalId, queryId);

        if (queryResult == null)
        {
            throw new AzureClientException($"QueryByIdAsync failed for {azureUri.Connection}, {project.InternalId}, {queryId}");
        }

        var workItemIds = new List<int>();

        // The WorkItems collection and individual reference objects may be null.
        switch (queryResult.QueryType)
        {
            // Tree types are treated as flat, but the data structure is different.
            case TFModels.QueryType.Tree:
                if (queryResult.WorkItemRelations is not null)
                {
                    foreach (var workItemRelation in queryResult.WorkItemRelations)
                    {
                        if (workItemRelation is null || workItemRelation.Target is null)
                        {
                            continue;
                        }

                        workItemIds.Add(workItemRelation.Target.Id);

                        // Query result limit
                        if (workItemIds.Count >= 25)
                        {
                            break;
                        }
                    }
                }

                break;

            case TFModels.QueryType.Flat:
                if (queryResult.WorkItems is not null)
                {
                    foreach (var item in queryResult.WorkItems)
                    {
                        if (item is null)
                        {
                            continue;
                        }

                        workItemIds.Add(item.Id);
                        if (workItemIds.Count >= 25)
                        {
                            break;
                        }
                    }
                }

                break;

            case TFModels.QueryType.OneHop:

                // OneHop work item structure is the same as the tree type.
                goto case TFModels.QueryType.Tree;

            default:
                break;
        }

        var workItems = new List<TFModels.WorkItem>();
        if (workItemIds.Count > 0)
        {
            workItems = await witClient.GetWorkItemsAsync(project.InternalId, workItemIds, null, null, TFModels.WorkItemExpand.Links, TFModels.WorkItemErrorPolicy.Omit);
            if (workItems == null)
            {
                throw new AzureClientException($"GetWorkItemsAsync failed for {azureUri.Connection}, {project.InternalId}, Ids: {string.Join(",", workItemIds.ToArray())}");
            }
        }

        var workItemsList = new List<WorkItem>();

        foreach (var workItem in workItems)
        {
            var fieldValue = workItem.Fields["System.WorkItemType"].ToString();
            var workItemTypeInfo = await witClient!.GetWorkItemTypeAsync(project.InternalId, fieldValue);
            var cmdPalWorkItem = WorkItem.GetOrCreate(_dataStore, workItem, result.Connection, project.Id, workItemTypeInfo);
            workItemsList.Add(cmdPalWorkItem);
        }

        stopwatch.Stop(); // Stop measuring time
        _log.Information($"GetWorkItems took {stopwatch.ElapsedMilliseconds} ms to complete.");

        return workItemsList;
    }

    public DataModel.Query? GetQuery(IQuery query)
    {
        ValidateDataStore();
        var account = _accountProvider.GetDefaultAccount();
        return DataModel.Query.Get(_dataStore, query.Url, account.Username);
    }

    private async Task<TeamProject> GetTeamProject(string projectName, IAccount account, Uri connection)
    {
        var result = _azureClientProvider.GetVssConnection(connection, account);

        if (result.Result != ResultType.Success)
        {
            if (result.Exception != null)
            {
                throw result.Exception;
            }
            else
            {
                throw new AzureAuthorizationException($"Failed getting connection: {connection} for {account.Username} with {result.Error}");
            }
        }

        var projectClient = new ProjectHttpClient(result.Connection!.Uri, result.Connection!.Credentials);
        if (projectClient == null)
        {
            throw new AzureClientException($"Failed getting ProjectHttpClient for {connection}");
        }

        var project = await projectClient.GetProject(projectName);
        if (project == null)
        {
            throw new AzureClientException($"Project reference was null for {connection} and Project: {projectName}");
        }

        return project;
    }
}
