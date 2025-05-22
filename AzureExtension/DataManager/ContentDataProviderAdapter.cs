// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls;
using AzureExtension.DataManager.Cache;
using AzureExtension.Helpers;

namespace AzureExtension.DataManager;

#pragma warning disable SA1649 // File name should match first type name
public class ContentDataProviderAdapter<TContentData> : ILiveContentDataProvider<TContentData>
{
    private readonly ILiveDataProvider _liveDataProvider;

    public ContentDataProviderAdapter(ILiveDataProvider liveDataProvider)
    {
        _liveDataProvider = liveDataProvider;
    }

    public event EventHandler<CacheManagerUpdateEventArgs> WeakOnUpdate
    {
        add => _liveDataProvider.WeakOnUpdate += value;
        remove => _liveDataProvider.WeakOnUpdate -= value;
    }

    public event EventHandler<CacheManagerUpdateEventArgs>? OnUpdate
    {
        add => _liveDataProvider.OnUpdate += value;
        remove => _liveDataProvider.OnUpdate -= value;
    }

    public Task<IEnumerable<TContentData>> GetContentData(IAzureSearch search)
    {
        return _liveDataProvider.GetContentData<TContentData>(search);
    }
}
