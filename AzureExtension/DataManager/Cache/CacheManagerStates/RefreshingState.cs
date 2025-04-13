// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace AzureExtension.DataManager.Cache;

public class RefreshingState : CacheManagerState
{
    public RefreshingState(CacheManager cacheManager)
        : base(cacheManager)
    {
    }

    public override Task Refresh(DataUpdateParameters dataUpdateParameters)
    {
        lock (CacheManager.GetStateLock())
        {
            var currentParameters = CacheManager.CurrentUpdateParameters;
            if (dataUpdateParameters.UpdateType == currentParameters?.UpdateType
                && dataUpdateParameters.UpdateObject == currentParameters?.UpdateObject)
            {
                Logger.Information("Search is the same as the pending search. Ignoring.");
                return Task.CompletedTask;
            }

            CacheManager.CurrentUpdateParameters = dataUpdateParameters;
        }

        CacheManager.CancelUpdateInProgress();

        lock (CacheManager.GetStateLock())
        {
            CacheManager.State = CacheManager.PendingRefreshState;
        }

        return Task.CompletedTask;
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
