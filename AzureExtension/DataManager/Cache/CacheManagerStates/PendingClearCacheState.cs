// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace AzureExtension.DataManager.Cache;

public class PendingClearCacheState : CacheManagerState
{
    public PendingClearCacheState(CacheManager cacheManager)
        : base(cacheManager)
    {
    }

    public override Task Refresh(DataUpdateParameters dataUpdateParameters)
    {
        return Task.CompletedTask;
    }

    public override void HandleDataManagerUpdate(object? source, DataManagerUpdateEventArgs e)
    {
        // We are expecting a cancel event. But anything else means we are done with an update.
        CacheManager.PurgeAllData();
        CacheManager.State = CacheManager.IdleState;
    }

    public override void ClearCache()
    {
        // No action needed
    }
}
