// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls;
using AzureExtension.DataManager.Cache;
using AzureExtension.Helpers;
using Serilog;

namespace AzureExtension.DataManager;

public class LiveDataProvider : ILiveDataProvider
{
    private readonly ILogger _log;

    private readonly ICacheManager _cacheManager;
    private readonly IDictionary<Type, IContentDataProvider> _contentProvidersDictionary;
    private readonly IDictionary<Type, ISearchDataProvider> _searchDataProvidersDictionary;

    private readonly WeakEventSource<CacheManagerUpdateEventArgs> _weakOnUpdate;

    public event EventHandler<CacheManagerUpdateEventArgs>? WeakOnUpdate
    {
        add => _weakOnUpdate.Subscribe(value);
        remove => _weakOnUpdate.Unsubscribe(value);
    }

    public event EventHandler<CacheManagerUpdateEventArgs>? OnUpdate;

    public LiveDataProvider(ICacheManager cacheManager, IDictionary<Type, IContentDataProvider> providersDictionary, IDictionary<Type, ISearchDataProvider> searchDataProvidersDictionary)
    {
        _log = Log.ForContext("SourceContext", nameof(ILiveDataProvider));
        _cacheManager = cacheManager;
        _contentProvidersDictionary = providersDictionary;
        _searchDataProvidersDictionary = searchDataProvidersDictionary;
        _weakOnUpdate = new WeakEventSource<CacheManagerUpdateEventArgs>();
        _cacheManager.OnUpdate += OnCacheManagerUpdate;
    }

    public void OnCacheManagerUpdate(object? source, CacheManagerUpdateEventArgs e)
    {
        _weakOnUpdate.Raise(source, e);
        OnUpdate?.Invoke(source, e);
    }

    private async Task WaitForCacheUpdateAsync(DataUpdateParameters parameters)
    {
        var tcs = new TaskCompletionSource();

        CacheManagerUpdateEventHandler handler = null!;
        handler = (sender, args) =>
        {
            _cacheManager.OnUpdate -= handler;
            tcs.TrySetResult();
        };

        _cacheManager.OnUpdate += handler;
        _ = _cacheManager.RequestRefresh(parameters);

        await tcs.Task;
    }

    private async Task WaitForLoadingDataIfNull(object? dataStoreObject, DataUpdateParameters parameters)
    {
        if (dataStoreObject == null)
        {
            await WaitForCacheUpdateAsync(parameters);
        }
        else
        {
            _ = _cacheManager.RequestRefresh(parameters);
        }
    }

    private DataUpdateType GetUpdateTypeForSearch(IAzureSearch search)
    {
        return search switch
        {
            IQuerySearch => DataUpdateType.Query,
            IPullRequestSearch => DataUpdateType.PullRequests,
            IPipelineDefinitionSearch => DataUpdateType.Pipeline,
            _ => throw new NotSupportedException($"No provider found for {search.GetType()}"),
        };
    }

    public async Task<IEnumerable<TContentDataType>> GetContentData<TContentDataType>(IAzureSearch search)
    {
        var searchType = search.GetSearchType();

        var contentProvider = _contentProvidersDictionary[searchType];
        var searchDataProvider = _searchDataProvidersDictionary[searchType];

        var searchDataObject = searchDataProvider.GetDataForSearch(search);
        await WaitForLoadingDataIfNull(searchDataObject, new DataUpdateParameters
        {
            UpdateType = GetUpdateTypeForSearch(search),
            UpdateObject = search,
        });
        return contentProvider.GetDataObjects(search).Cast<TContentDataType>();
    }

    public async Task<TSearchDataType> GetSearchData<TSearchDataType>(IAzureSearch search)
    {
        var searchType = search.GetSearchType();

        var searchDataProvider = _searchDataProvidersDictionary[searchType];

        var searchDataObject = searchDataProvider.GetDataForSearch(search);
        await WaitForLoadingDataIfNull(searchDataObject, new DataUpdateParameters
        {
            UpdateType = GetUpdateTypeForSearch(search),
            UpdateObject = search,
        });
        return (TSearchDataType)searchDataProvider.GetDataForSearch(search)!;
    }
}
