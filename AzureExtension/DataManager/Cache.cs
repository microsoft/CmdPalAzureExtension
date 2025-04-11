// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Data;
using AzureExtension.DataModel;
using Microsoft.VisualStudio.Services.WebApi;

namespace AzureExtension.DataManager;

public class Cache
{
    private readonly DataStore _dataStore;

    public Cache(DataStore dataStore)
    {
        _dataStore = dataStore;
    }

    private void ValidateDataStore()
    {
        if (_dataStore == null || !_dataStore.IsConnected)
        {
            throw new DataStoreInaccessibleException("Cache DataStore is not available.");
        }
    }

    public Identity GetIdentity(IdentityRef identityRef, VssConnection connection)
    {
        ValidateDataStore();
        return Identity.GetOrCreateIdentity(_dataStore, identityRef, connection);
    }
}
