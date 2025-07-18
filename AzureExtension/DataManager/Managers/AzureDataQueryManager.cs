﻿// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Account;
using AzureExtension.Client;
using AzureExtension.Controls;
using AzureExtension.Data;
using AzureExtension.DataModel;
using Serilog;
using Query = AzureExtension.DataModel.Query;
using TFModels = Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using WorkItem = AzureExtension.DataModel.WorkItem;

namespace AzureExtension.DataManager;

public class AzureDataQueryManager
    : ISearchDataProvider<IQuerySearch, Query>, IContentDataProvider<IQuerySearch, WorkItem>, ISearchDataProvider, IContentDataProvider,  IDataUpdater
{
    private readonly TimeSpan _queryWorkItemDeletionTime = TimeSpan.FromMinutes(2);

    private readonly ILogger _log;
    private readonly DataStore _dataStore;
    private readonly IAccountProvider _accountProvider;
    private readonly IAzureLiveDataProvider _liveDataProvider;
    private readonly IConnectionProvider _connectionProvider;
    private readonly ISavedSearchesSource<IQuerySearch> _queryRepository;

    private const int AzureAPIWorkItemLimit = 200;

    public AzureDataQueryManager(
        DataStore dataStore,
        IAccountProvider accountProvider,
        IAzureLiveDataProvider liveDataProvider,
        IConnectionProvider connectionProvider,
        ISavedSearchesSource<IQuerySearch> queryRepository)
    {
        _dataStore = dataStore;
        _accountProvider = accountProvider;
        _log = Serilog.Log.ForContext("SourceContext", nameof(AzureDataQueryManager));
        _liveDataProvider = liveDataProvider;
        _connectionProvider = connectionProvider;
        _queryRepository = queryRepository;
    }

    private void ValidateDataStore()
    {
        if (_dataStore == null || !_dataStore.IsConnected)
        {
            throw new DataStoreInaccessibleException("Cache DataStore is not available.");
        }
    }

    public Query? GetDataForSearch(IQuerySearch query)
    {
        ValidateDataStore();
        var account = _accountProvider.GetDefaultAccount();
        var azureUri = new AzureUri(query.Url);
        return Query.Get(_dataStore, azureUri.Query, account.Username);
    }

    public bool IsNewOrStale(IQuerySearch query, TimeSpan refreshCooldown)
    {
        var dsQuery = GetDataForSearch(query);
        return dsQuery == null || DateTime.UtcNow - dsQuery.UpdatedAt > refreshCooldown;
    }

    public bool IsNewOrStale(DataUpdateParameters parameters, TimeSpan refreshCooldown)
    {
        return IsNewOrStale((parameters.UpdateObject as IQuerySearch)!, refreshCooldown);
    }

    public IEnumerable<WorkItem> GetDataObjects(IQuerySearch query)
    {
        ValidateDataStore();
        var dsQuery = GetDataForSearch(query);
        return dsQuery != null ? WorkItem.GetForQuery(_dataStore, dsQuery) : [];
    }

    public object? GetDataForSearch(IAzureSearch search)
    {
        return GetDataForSearch(search as IQuerySearch ?? throw new InvalidOperationException("Invalid search type"));
    }

    public IEnumerable<object> GetDataObjects(IAzureSearch search)
    {
        return GetDataObjects(search as IQuerySearch ?? throw new InvalidOperationException("Invalid search type"));
    }

    public async Task UpdateQueryAsync(IQuerySearch query, CancellationToken cancellationToken)
    {
        var azureUri = new AzureUri(query.Url);

        var account = await _accountProvider.GetDefaultAccountAsync();

        var vssConnection = await _connectionProvider.GetVssConnectionAsync(azureUri.Connection, account);

        // Good practice to only create data after we know the client is valid, but any exceptions
        // will roll back the transaction.
        var org = Organization.GetOrCreate(_dataStore, azureUri.Connection);

        var project = Project.Get(_dataStore, azureUri.Project, org.Id);
        if (project is null)
        {
            var teamProject = await _liveDataProvider.GetTeamProject(vssConnection, azureUri.Project);
            project = Project.GetOrCreateByTeamProject(_dataStore, teamProject, org.Id);
        }

        var queryId = new Guid(azureUri.Query);

        var queryResult = await _liveDataProvider.GetWorkItemQueryResultByIdAsync(vssConnection, project.InternalId, queryId, cancellationToken);

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
            var workItemIdChunks = workItemIds.Chunk(AzureAPIWorkItemLimit);
            var chunkedWorkItemsTasks = new List<Task<List<TFModels.WorkItem>>>();
            foreach (var chunk in workItemIdChunks)
            {
                var chunkedWorkItemsTask = _liveDataProvider.GetWorkItemsAsync(vssConnection, project.InternalId, chunk, TFModels.WorkItemExpand.Links, TFModels.WorkItemErrorPolicy.Omit, cancellationToken);
                chunkedWorkItemsTasks.Add(chunkedWorkItemsTask);
            }

            foreach (var task in chunkedWorkItemsTasks)
            {
                var chunkedWorkItems = await task;
                if (chunkedWorkItems != null && chunkedWorkItems.Count > 0)
                {
                    workItems.AddRange(chunkedWorkItems);
                }
            }
        }

        var dsQuery = Query.GetOrCreate(_dataStore, azureUri.Query, project.Id, account.Username, query.Name);

        var workItemTasks = new List<Task<TFModels.WorkItemType>>();
        foreach (var workItem in workItems)
        {
            var fieldValue = workItem.Fields["System.WorkItemType"].ToString();
            var wiTask = _liveDataProvider.GetWorkItemTypeAsync(vssConnection, project.InternalId, fieldValue, cancellationToken);
            workItemTasks.Add(wiTask);
        }

        for (var i = 0; i < workItemTasks.Count; i++)
        {
            var task = workItemTasks[i];
            var workItem = workItems[i];

            var workItemTypeInfo = await task;
            var cmdPalWorkItem = WorkItem.GetOrCreate(_dataStore, workItem, vssConnection, _liveDataProvider, project.Id, workItemTypeInfo);
            QueryWorkItem.AddWorkItemToQuery(_dataStore, dsQuery.Id, cmdPalWorkItem.Id);
        }

        QueryWorkItem.DeleteBefore(_dataStore, dsQuery, DateTime.UtcNow - _queryWorkItemDeletionTime);
    }

    private readonly TimeSpan _queryRetentionTime = TimeSpan.FromDays(7);

    public void PruneObsoleteData()
    {
        Query.DeleteBefore(_dataStore, DateTime.UtcNow - _queryRetentionTime);
        QueryWorkItem.DeleteUnreferenced(_dataStore);
    }

    public async Task UpdateData(DataUpdateParameters parameters)
    {
        if (parameters.UpdateType == DataUpdateType.All)
        {
            var queries = _queryRepository.GetSavedSearches();
            foreach (var query in queries)
            {
                await UpdateQueryAsync(query, parameters.CancellationToken.GetValueOrDefault());
            }

            return;
        }

        await UpdateQueryAsync((parameters.UpdateObject as IQuerySearch)!, parameters.CancellationToken.GetValueOrDefault());
    }
}
