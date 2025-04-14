// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls;
using AzureExtension.Data;
using AzureExtension.DataManager.Cache;
using AzureExtension.DataModel;
using Serilog;

namespace AzureExtension.DataManager;

public class DataProvider : IDataProvider
{
    private readonly ILogger _log;
    private readonly IDataObjectProvider _dataObjectProvider;
    private readonly ICacheManager _cacheManager;

    public event CacheManagerUpdateEventHandler? OnUpdate;

    public DataProvider(IDataObjectProvider dataObjectProvider, ICacheManager cacheManager)
    {
        _log = Log.ForContext("SourceContext", nameof(IDataProvider));
        _cacheManager = cacheManager;
        _dataObjectProvider = dataObjectProvider;

        _cacheManager.OnUpdate += OnCacheManagerUpdate;
    }

    public async Task<IEnumerable<IWorkItem>> GetWorkItems(IQuery query)
    {
        var dsQuery = _dataObjectProvider.GetQuery(query);
        if (dsQuery == null)
        {
            var parameters = new DataUpdateParameters
            {
                UpdateType = DataUpdateType.Query,
                UpdateObject = query,
            };
            await _cacheManager.RequestRefresh(parameters);
        }

        return _dataObjectProvider.GetWorkItems(query);
    }

    public void OnCacheManagerUpdate(object? source, CacheManagerUpdateEventArgs e)
    {
        OnUpdate?.Invoke(source, e);
    }
}
