// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls;
using AzureExtension.DataManager.Cache;
using AzureExtension.DataModel;
using Serilog;
using PullRequestSearch = AzureExtension.DataModel.PullRequestSearch;
using Query = AzureExtension.DataModel.Query;
using WorkItem = AzureExtension.DataModel.WorkItem;

namespace AzureExtension.DataManager;

public class LiveDataProvider : ILiveDataProvider
{
    private readonly ILogger _log;

    private readonly ICacheManager _cacheManager;
    private readonly IDataProvider<IQuery, Query, WorkItem> _queryProvider;
    private readonly IDataProvider<IPullRequestSearch, PullRequestSearch, PullRequest> _pullRequestSearchProvider;
    private readonly IDataProvider<IPipelineDefinitionSearch, Definition, Build> _pipelineProvider;

    private CacheManagerUpdateEventHandler? _onUpdate;

    public event CacheManagerUpdateEventHandler? OnUpdate
    {
        add => _onUpdate = value;
        remove => _onUpdate -= value;
    }

    public LiveDataProvider(
        ICacheManager cacheManager,
        IDataProvider<IQuery, Query, WorkItem> queryProvider,
        IDataProvider<IPullRequestSearch, PullRequestSearch, PullRequest> pullRequestSearchProvider,
        IDataProvider<IPipelineDefinitionSearch, Definition, Build> pipelineProvider)
    {
        _log = Log.ForContext("SourceContext", nameof(ILiveDataProvider));
        _cacheManager = cacheManager;
        _queryProvider = queryProvider;
        _pullRequestSearchProvider = pullRequestSearchProvider;
        _pipelineProvider = pipelineProvider;

        _cacheManager.OnUpdate += OnCacheManagerUpdate;
    }

    public void OnCacheManagerUpdate(object? source, CacheManagerUpdateEventArgs e)
    {
        _onUpdate?.Invoke(source, e);
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

    public async Task<IEnumerable<IWorkItem>> GetWorkItems(IQuery query)
    {
        var parameters = new DataUpdateParameters
        {
            UpdateType = DataUpdateType.Query,
            UpdateObject = query,
        };

        var dsQuery = _queryProvider.GetDataForSearch(query);
        await WaitForLoadingDataIfNull(dsQuery, parameters);

        return _queryProvider.GetDataObjects(query);
    }

    public async Task<IEnumerable<IPullRequest>> GetPullRequests(IPullRequestSearch pullRequestSearch)
    {
        var parameters = new DataUpdateParameters
        {
            UpdateType = DataUpdateType.PullRequests,
            UpdateObject = pullRequestSearch,
        };

        var dsPullRequestSearch = _pullRequestSearchProvider.GetDataForSearch(pullRequestSearch);
        await WaitForLoadingDataIfNull(dsPullRequestSearch, parameters);

        return _pullRequestSearchProvider.GetDataObjects(pullRequestSearch);
    }

    public async Task<IEnumerable<IBuild>> GetBuilds(IPipelineDefinitionSearch definitionSearch)
    {
        var parameters = new DataUpdateParameters
        {
            UpdateType = DataUpdateType.Pipeline,
            UpdateObject = definitionSearch,
        };

        var dsDefinition = _pipelineProvider.GetDataForSearch(definitionSearch);
        await WaitForLoadingDataIfNull(dsDefinition, parameters);
        return _pipelineProvider.GetDataObjects(definitionSearch);
    }

    public async Task<IDefinition> GetDefinition(IPipelineDefinitionSearch definitionSearch)
    {
        var parameters = new DataUpdateParameters
        {
            UpdateType = DataUpdateType.Pipeline,
            UpdateObject = definitionSearch,
        };

        var dsDefinition = _pipelineProvider.GetDataForSearch(definitionSearch);
        await WaitForLoadingDataIfNull(dsDefinition, parameters);
        return _pipelineProvider.GetDataForSearch(definitionSearch)!;
    }
}
