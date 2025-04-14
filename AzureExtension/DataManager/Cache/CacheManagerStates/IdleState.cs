// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace AzureExtension.DataManager.Cache;

public class IdleState : CacheManagerState
{
    public IdleState(CacheManager cacheManager)
        : base(cacheManager)
    {
    }

    public async override Task Refresh(DataUpdateParameters dataUpdateParameters)
    {
        lock (CacheManager.GetStateLock())
        {
            CacheManager.State = CacheManager.RefreshingState;
            CacheManager.CurrentUpdateParameters = dataUpdateParameters;
        }

        Logger.Information($"Starting refresh for : {dataUpdateParameters}");
        await CacheManager.Update(dataUpdateParameters);
    }

    public async override Task PeriodicUpdate()
    {
        // Only update per the update interval.
        if (DateTime.UtcNow - CacheManager.LastUpdateTime < CacheManager.UpdateInterval)
        {
            Logger.Information("Not time for periodic update.");
            return;
        }

        lock (CacheManager.GetStateLock())
        {
            CacheManager.State = CacheManager.PeriodicUpdatingState;
        }

        var parameters = new DataUpdateParameters
        {
            UpdateType = DataUpdateType.All,
        };

        Logger.Information("Starting periodic update.");
        await CacheManager.Update(parameters);
    }
}
