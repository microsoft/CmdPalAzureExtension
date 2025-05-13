// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.DataManager.Cache;

namespace AzureExtension.Controls;

public interface ILiveDataProvider
{
    Task<IEnumerable<TContentDataType>> GetContentData<TContentDataType>(IAzureSearch search);

    Task<TSearchDataType> GetSearchData<TSearchDataType>(IAzureSearch search);

    event CacheManagerUpdateEventHandler? OnUpdate;
}
