// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace AzureExtension.DataManager.Cache;

public interface IDataUpdateService
{
    DateTime LastUpdated { get; set; }

    event DataManagerUpdateEventHandler? OnUpdate;

    bool IsNewOrStaleData(DataUpdateParameters parameters, TimeSpan refreshCooldown);

    Task UpdateData(DataUpdateParameters parameters);

    void PurgeAllData();
}
