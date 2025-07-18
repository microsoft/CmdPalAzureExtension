﻿// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls;
using AzureExtension.DataManager.Cache;

namespace AzureExtension.DataManager;

#pragma warning disable SA1649 // File name should match first type name
public class SearchDataProviderAdapter<TSearchDataType>
    : ILiveSearchDataProvider<TSearchDataType>
{
    private readonly ILiveDataProvider _liveDataProvider;

    public SearchDataProviderAdapter(ILiveDataProvider liveDataProvider)
    {
        _liveDataProvider = liveDataProvider;
    }

    public Task<TSearchDataType> GetSearchData(IAzureSearch search)
    {
        return _liveDataProvider.GetSearchData<TSearchDataType>(search);
    }
}
