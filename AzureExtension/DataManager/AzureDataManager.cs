// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Data;
using AzureExtension.DataManager.Cache;
using AzureExtension.DataModel;
using AzureExtension.Helpers;
using Serilog;

namespace AzureExtension.DataManager;

public class AzureDataManager : IDataUpdateService
{
    private readonly ILogger _log;
    private readonly DataStore _dataStore;
    private readonly IDictionary<DataUpdateType, IDataUpdater> _dataUpdaters;

    public AzureDataManager(DataStore dataStore, IDictionary<DataUpdateType, IDataUpdater> dataUpdaters)
    {
        _log = Log.ForContext("SourceContext", nameof(AzureDataManager));
        _dataStore = dataStore;
        _dataUpdaters = dataUpdaters;
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

    private async Task PerformUpdateAsync(DataUpdateParameters parameters, Func<Task> asyncOperation)
    {
        using var tx = _dataStore.Connection!.BeginTransaction();

        try
        {
            await asyncOperation();

            // SetLastUpdatedInMetaData();
        }
        catch (Exception ex) when (IsCancelException(ex))
        {
            tx.Rollback();
            OnUpdate?.Invoke(this, new DataManagerUpdateEventArgs(DataManagerUpdateKind.Cancel, parameters, ex));
            _log.Information($"Update cancelled: {parameters.ToString()}");
            return;
        }
        catch (Exception ex)
        {
            tx.Rollback();
            _log.Error(ex, $"Error during update: {ex.Message}");
            return;
        }

        tx.Commit();
        _log.Information($"Update complete: {parameters.ToString()}");
        OnUpdate?.Invoke(this, new DataManagerUpdateEventArgs(DataManagerUpdateKind.Success, parameters));
    }

    public async Task UpdateData(DataUpdateParameters parameters)
    {
        var type = parameters.UpdateType;

        Func<Task> updateOperation;

        if (type == DataUpdateType.All)
        {
            updateOperation = async () =>
            {
                foreach (var updater in _dataUpdaters.Values)
                {
                    await updater.UpdateData(parameters);
                    updater.PruneObsoleteData();
                }
            };
        }
        else
        {
            if (!_dataUpdaters.TryGetValue(type, out var updater))
            {
                throw new NotImplementedException($"Update type {type} not implemented.");
            }

            updateOperation = async () =>
            {
                await updater.UpdateData(parameters);
                updater.PruneObsoleteData();
            };
        }

        await PerformUpdateAsync(parameters, updateOperation);
    }

    public bool IsNewOrStaleData(DataUpdateParameters parameters, TimeSpan refreshCooldown)
    {
        if (_dataUpdaters.TryGetValue(parameters.UpdateType, out var updater))
        {
            return updater.IsNewOrStale(parameters, refreshCooldown);
        }

        throw new NotImplementedException($"Update type {parameters.UpdateType} not implemented.");
    }

    public void PurgeAllData()
    {
        _dataStore.Reset();
    }
}
