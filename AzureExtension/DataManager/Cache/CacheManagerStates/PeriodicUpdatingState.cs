// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace AzureExtension.DataManager.Cache;

public class PeriodicUpdatingState : CacheManagerState
{
    public PeriodicUpdatingState(CacheManager cacheManager)
        : base(cacheManager)
    {
    }

    public async override Task Refresh(DataUpdateParameters dataUpdateParameters)
    {
        await Task.Run(() =>
        {
            CacheManager.CancelUpdateInProgress();

            lock (CacheManager.GetStateLock())
            {
                CacheManager.CurrentUpdateParameters = dataUpdateParameters;
                CacheManager.State = CacheManager.PendingRefreshState;
            }
        });
    }

    public override void HandleDataManagerUpdate(object? source, DataManagerUpdateEventArgs e)
    {
        Logger.Information("Received data manager update event. Changing to Idle state.");
        lock (CacheManager.GetStateLock())
        {
            CacheManager.State = CacheManager.IdleState;
            CacheManager.CurrentUpdateParameters = null;
        }
    }
}
