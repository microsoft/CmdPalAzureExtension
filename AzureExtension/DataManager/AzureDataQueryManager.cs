// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Account;
using AzureExtension.Client;
using AzureExtension.Controls;
using AzureExtension.Data;
using AzureExtension.DataModel;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Serilog;
using Query = AzureExtension.DataModel.Query;
using TFModels = Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using WorkItem = AzureExtension.DataModel.WorkItem;

namespace AzureExtension.DataManager;

public class AzureDataQueryManager : IDataQueryUpdater, IDataQueryProvider
{
    private readonly ILogger _log;
    private readonly DataStore _dataStore;
    private readonly IAccountProvider _accountProvider;
    private readonly AzureClientProvider _azureClientProvider;

    public AzureDataQueryManager(DataStore dataStore, IAccountProvider accountProvider, AzureClientProvider azureClientProvider)
    {
        _dataStore = dataStore;
        _accountProvider = accountProvider;
        _log = Serilog.Log.ForContext("SourceContext", nameof(AzureDataQueryManager));
        _azureClientProvider = azureClientProvider;
    }

    private void ValidateDataStore()
    {
        if (_dataStore == null || !_dataStore.IsConnected)
        {
            throw new DataStoreInaccessibleException("Cache DataStore is not available.");
        }
    }

    public Query? GetQuery(IQuery query)
    {
        ValidateDataStore();
        var account = _accountProvider.GetDefaultAccount();
        var azureUri = new AzureUri(query.Url);
        return Query.Get(_dataStore, azureUri.Query, account.Username);
    }

    public bool IsNewOrStale(IQuery query, TimeSpan refreshCooldown)
    {
        var dsQuery = GetQuery(query);
        return dsQuery == null || DateTime.UtcNow - dsQuery.UpdatedAt > refreshCooldown;
    }

    public IEnumerable<IWorkItem> GetWorkItems(IQuery query)
    {
        ValidateDataStore();
        var dsQuery = GetQuery(query);
        return dsQuery != null ? WorkItem.GetForQuery(_dataStore, dsQuery) : [];
    }

    public async Task UpdateQueryAsync(IQuery query, CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew(); // Start measuring time

        var azureUri = new AzureUri(query.Url);
        var account = _accountProvider.GetDefaultAccount();
        var connection = _azureClientProvider.GetVssConnection(azureUri.Connection, account);

        var witClient = _azureClientProvider.GetClient<WorkItemTrackingHttpClient>(azureUri.Connection, account);

        // Good practice to only create data after we know the client is valid, but any exceptions
        // will roll back the transaction.
        var org = Organization.GetOrCreate(_dataStore, azureUri.Connection);

        var project = Project.Get(_dataStore, azureUri.Project, org.Id);
        if (project is null)
        {
            var projectClient = _azureClientProvider.GetClient<ProjectHttpClient>(azureUri.Connection, account);
            var teamProject = await projectClient.GetProject(azureUri.Project);
            project = Project.GetOrCreateByTeamProject(_dataStore, teamProject, org.Id);
        }

        var queryId = new Guid(azureUri.Query);
        var queryResult = await witClient.QueryByIdAsync(project.InternalId, queryId, cancellationToken: cancellationToken);

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
            workItems = await witClient.GetWorkItemsAsync(project.InternalId, workItemIds, null, null, TFModels.WorkItemExpand.Links, TFModels.WorkItemErrorPolicy.Omit, cancellationToken: cancellationToken);
        }

        var workItemsList = new List<WorkItem>();
        var dsQuery = Query.GetOrCreate(_dataStore, azureUri.Query, project.Id, account.Username, query.Name);

        foreach (var workItem in workItems)
        {
            var fieldValue = workItem.Fields["System.WorkItemType"].ToString();
            var workItemTypeInfo = await witClient.GetWorkItemTypeAsync(project.InternalId, fieldValue, cancellationToken: cancellationToken);
            var cmdPalWorkItem = WorkItem.GetOrCreate(_dataStore, workItem, connection, project.Id, workItemTypeInfo);
            QueryWorkItem.AddWorkItemToQuery(_dataStore, dsQuery.Id, cmdPalWorkItem.Id);
            workItemsList.Add(cmdPalWorkItem);
        }

        QueryWorkItem.DeleteBefore(_dataStore, dsQuery, DateTime.UtcNow - TimeSpan.FromMinutes(2));

        stopwatch.Stop(); // Stop measuring time
        _log.Information($"UpdateWorkItems took {stopwatch.ElapsedMilliseconds} ms to complete.");
    }
}
