// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace AzureExtension.DataManager;

public interface IDataUpdateService
{
    DateTime LastUpdated { get; set; }

    event DataManagerUpdateEventHandler? OnUpdate;

    Task UpdateData(DataUpdateParameters parameters);
}
