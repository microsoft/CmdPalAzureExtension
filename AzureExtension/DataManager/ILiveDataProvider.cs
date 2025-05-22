// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls;
using AzureExtension.DataManager.Cache;
using AzureExtension.Helpers;

namespace AzureExtension.DataManager;

public interface ILiveDataProvider
{
    Task<IEnumerable<TContentDataType>> GetContentData<TContentDataType>(IAzureSearch search);

    Task<TSearchDataType> GetSearchData<TSearchDataType>(IAzureSearch search);

    WeakEvent<CacheManagerUpdateEventArgs> WeakOnUpdate { get; }

    event CacheManagerUpdateEventHandler? OnUpdate;
}
