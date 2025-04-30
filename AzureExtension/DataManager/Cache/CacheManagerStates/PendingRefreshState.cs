// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace AzureExtension.DataManager.Cache;

public class PendingRefreshState : CacheManagerState
{
    public PendingRefreshState(CacheManager cacheManager)
        : base(cacheManager)
    {
    }

    public async override Task Refresh(DataUpdateParameters dataUpdateParameters)
    {
        await Task.Run(() =>
        {
            var currentParameters = CacheManager.CurrentUpdateParameters;
            if (dataUpdateParameters.UpdateType == currentParameters?.UpdateType
                && dataUpdateParameters.UpdateObject == currentParameters?.UpdateObject)
            {
                Logger.Information("Search is the same as the pending parameters. Ignoring.");
                return;
            }

            CacheManager.CurrentUpdateParameters = dataUpdateParameters;
            CacheManager.CancelUpdateInProgress();
        });
    }

    public async override void HandleDataManagerUpdate(object? source, DataManagerUpdateEventArgs e)
    {
        switch (e.Kind)
        {
            case DataManagerUpdateKind.Cancel:
                Logger.Information($"Received data manager cancellation. Refreshing for {CacheManager.CurrentUpdateParameters!.UpdateType}");
                CacheManager.State = CacheManager.RefreshingState;

                await CacheManager.Update(CacheManager.CurrentUpdateParameters!);
                break;
            default:
                Logger.Information($"Received data manager update event {e.Kind}. Changing to Idle state.");

                CacheManager.State = CacheManager.IdleState;
                CacheManager.CurrentUpdateParameters = null;
                break;
        }
    }
}
