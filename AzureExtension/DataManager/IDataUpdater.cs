// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace AzureExtension.DataManager;

public interface IDataUpdater
{
    Task UpdateData(DataUpdateParameters parameters);

    bool IsNewOrStale(DataUpdateParameters parameters, TimeSpan refreshCooldown);
}
