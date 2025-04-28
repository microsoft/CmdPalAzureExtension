// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls;
using AzureExtension.DataManager.Cache;
using Serilog;

namespace AzureExtension.DataManager;

public class DataProvider : IDataProvider
{
    private readonly ILogger _log;

    private readonly ICacheManager _cacheManager;
    private readonly IDataQueryProvider _queryProvider;
    private readonly IDataPullRequestSearchProvider _pullRequestSearchProvider;

    public static readonly string IdentityRefFieldValueName = "Microsoft.VisualStudio.Services.WebApi.IdentityRef";
    public static readonly string SystemIdFieldName = "System.Id";
    public static readonly string WorkItemHtmlUrlFieldName = "DevHome.AzureExtension.WorkItemHtmlUrl";
    public static readonly string WorkItemTypeFieldName = "System.WorkItemType";

    public static readonly int PullRequestResultLimit = 25;

    private CacheManagerUpdateEventHandler? _onUpdate;

    public event CacheManagerUpdateEventHandler? OnUpdate
    {
        add => _onUpdate = value;
        remove => _onUpdate -= value;
    }

    public DataProvider(ICacheManager cacheManager, IDataQueryProvider queryProvider, IDataPullRequestSearchProvider pullRequestSearchProvider)
    {
        _log = Log.ForContext("SourceContext", nameof(IDataProvider));
        _cacheManager = cacheManager;
        _queryProvider = queryProvider;
        _pullRequestSearchProvider = pullRequestSearchProvider;

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

    public async Task<IEnumerable<IWorkItem>> GetWorkItems(IQuery query)
    {
        var parameters = new DataUpdateParameters
        {
            UpdateType = DataUpdateType.Query,
            UpdateObject = query,
        };

        var dsQuery = _queryProvider.GetQuery(query);
        if (dsQuery == null)
        {
            await WaitForCacheUpdateAsync(parameters);
        }
        else
        {
            _ = _cacheManager.RequestRefresh(parameters);
        }

        return _queryProvider.GetWorkItems(query);
    }

    public async Task<IEnumerable<IPullRequest>> GetPullRequests(IPullRequestSearch pullRequestSearch)
    {
        var parameters = new DataUpdateParameters
        {
            UpdateType = DataUpdateType.PullRequests,
            UpdateObject = pullRequestSearch,
        };

        var dsPullRequestSearch = _pullRequestSearchProvider.GetPullRequestSearch(pullRequestSearch);
        _ = _cacheManager.RequestRefresh(parameters);
        if (dsPullRequestSearch == null)
        {
            await WaitForCacheUpdateAsync(parameters);
        }
        else
        {
            _ = _cacheManager.RequestRefresh(parameters);
        }

        return _pullRequestSearchProvider.GetPullRequests(pullRequestSearch);
    }
}
