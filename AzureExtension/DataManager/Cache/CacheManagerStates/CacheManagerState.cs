// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Serilog;

namespace AzureExtension.DataManager.Cache;

public abstract class CacheManagerState
{
    protected CacheManager CacheManager { get; private set; }

    protected ILogger Logger { get; private set; }

    protected CacheManagerState(CacheManager cacheManager)
    {
        CacheManager = cacheManager;
        Logger = Log.Logger.ForContext("SourceContext", $"CacheManager/{GetType().Name}");
    }

    public abstract Task Refresh(DataUpdateParameters dataUpdateParameters);

    public virtual Task PeriodicUpdate()
    {
        Logger.Information("Periodic update requested. Ignoring.");
        return Task.CompletedTask;
    }

    public virtual void HandleDataManagerUpdate(object? source, DataManagerUpdateEventArgs e)
    {
        return;
    }

    public virtual void ClearCache()
    {
        CacheManager.State = CacheManager.PendingClearCacheState;
        CacheManager.CancelUpdateInProgress();
    }
}
