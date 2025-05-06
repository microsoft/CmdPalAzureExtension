// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls;
using AzureExtension.Data;
using AzureExtension.DataManager.Cache;
using AzureExtension.DataModel;
using AzureExtension.Helpers;
using Serilog;
using PullRequestSearch = AzureExtension.DataModel.PullRequestSearch;
using Query = AzureExtension.DataModel.Query;
using WorkItem = AzureExtension.DataModel.WorkItem;

namespace AzureExtension.DataManager;

public class AzureDataManager : IDataUpdateService
{
    private readonly ILogger _log;
    private readonly DataStore _dataStore;
    private readonly IDataPullRequestSearchUpdater _pullRequestSearchUpdater;
    private readonly IDataQueryUpdater _queryUpdater;
    private readonly IPipelineUpdater _pipelineUpdater;

    public AzureDataManager(
        DataStore dataStore,
        IDataQueryUpdater queryUpdater,
        IDataPullRequestSearchUpdater pullRequestSearchUpdater,
        IPipelineUpdater pipelineUpdater)
    {
        _log = Log.ForContext("SourceContext", nameof(AzureDataManager));
        _dataStore = dataStore;
        _queryUpdater = queryUpdater;
        _pullRequestSearchUpdater = pullRequestSearchUpdater;
        _pipelineUpdater = pipelineUpdater;
    }

    private void ValidateDataStore()
    {
        if (_dataStore == null || !_dataStore.IsConnected)
        {
            throw new DataStoreInaccessibleException("Cache DataStore is not available.");
        }
    }

    private const string LastUpdatedKeyName = "LastUpdated";

    public event DataManagerUpdateEventHandler? OnUpdate;

    public DateTime LastUpdated
    {
        get
        {
            ValidateDataStore();
            var lastUpdated = MetaData.Get(_dataStore, LastUpdatedKeyName);
            if (lastUpdated == null)
            {
                return DateTime.MinValue;
            }

            return lastUpdated.ToDateTime();
        }

        set
        {
            ValidateDataStore();
            MetaData.AddOrUpdate(_dataStore, LastUpdatedKeyName, value.ToDataStoreString());
        }
    }

    private static bool IsCancelException(Exception ex)
    {
        return (ex is OperationCanceledException) || (ex is TaskCanceledException);
    }

    private readonly TimeSpan _queryRetentionTime = TimeSpan.FromDays(7);
    private readonly TimeSpan _pullRequestSearchRetentionTime = TimeSpan.FromDays(7);
    private readonly TimeSpan _pipelineRetentionTime = TimeSpan.FromDays(7);

    // Removes unused data from the datastore.
    private void PruneObsoleteData()
    {
        Query.DeleteBefore(_dataStore, DateTime.UtcNow - _queryRetentionTime);
        PullRequestSearch.DeleteBefore(_dataStore, DateTime.UtcNow - _pullRequestSearchRetentionTime);
        QueryWorkItem.DeleteUnreferenced(_dataStore);
        PullRequestSearchPullRequest.DeleteUnreferenced(_dataStore);
        WorkItem.DeleteNotReferencedByQuery(_dataStore);
        PullRequest.DeleteNotReferencedBySearch(_dataStore);
        Build.DeleteBefore(_dataStore, DateTime.UtcNow - _pipelineRetentionTime);
        Definition.DeleteUnreferenced(_dataStore);
    }

    private async Task PerformUpdateAsync(DataUpdateParameters parameters, Func<Task> asyncOperation)
    {
        using var tx = _dataStore.Connection!.BeginTransaction();

        try
        {
            await asyncOperation();
            PruneObsoleteData();

            // SetLastUpdatedInMetaData();
        }
        catch (Exception ex) when (IsCancelException(ex))
        {
            tx.Rollback();
            OnUpdate?.Invoke(this, new DataManagerUpdateEventArgs(DataManagerUpdateKind.Cancel, parameters, ex));
            _log.Information($"Update cancelled: {parameters}");
            return;
        }
        catch (Exception ex)
        {
            tx.Rollback();
            _log.Error(ex, $"Error during update: {ex.Message}");
            return;
        }

        tx.Commit();
        _log.Information($"Update complete: {parameters}");
        OnUpdate?.Invoke(this, new DataManagerUpdateEventArgs(DataManagerUpdateKind.Success, parameters));
    }

    public async Task UpdateData(DataUpdateParameters parameters)
    {
        var type = parameters.UpdateType;

        Func<Task> operation = type switch
        {
            DataUpdateType.Query => async () => await _queryUpdater.UpdateQueryAsync((parameters.UpdateObject as IQuery)!, parameters.CancellationToken.GetValueOrDefault()),
            DataUpdateType.PullRequests => async () => await _pullRequestSearchUpdater.UpdatePullRequestsAsync((parameters.UpdateObject as IPullRequestSearch)!, parameters.CancellationToken.GetValueOrDefault()),
            DataUpdateType.Pipeline => async () => await _pipelineUpdater.UpdatePipelineAsync((parameters.UpdateObject as IDefinitionSearch)!, parameters.CancellationToken.GetValueOrDefault()),
            _ => throw new NotImplementedException($"Update type {type} not implemented."),
        };

        await PerformUpdateAsync(parameters, operation);
    }

    public bool IsNewOrStaleData(DataUpdateParameters parameters, TimeSpan refreshCooldown)
    {
        return parameters.UpdateObject switch
        {
            IQuery query => _queryUpdater.IsNewOrStale(query, refreshCooldown),
            IPullRequestSearch pullRequestSearch => _pullRequestSearchUpdater.IsNewOrStale(pullRequestSearch, refreshCooldown),
            IDefinitionSearch pipeline => _pipelineUpdater.IsNewOrStale(pipeline, refreshCooldown),
            _ => throw new NotImplementedException($"Update type {parameters.UpdateType} not implemented."),
        };
    }
}
