// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Account;
using AzureExtension.Client;
using AzureExtension.Controls;
using AzureExtension.DataManager.Cache;
using AzureExtension.DataModel;
using AzureExtension.Helpers;
using AzureExtension.PersistentData;
using Microsoft.Identity.Client;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.Policy.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.VisualStudio.Services.Identity;
using Microsoft.VisualStudio.Services.WebApi;
using Serilog;
using Windows.UI.Notifications;
using TFModels = Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace AzureExtension.DataManager;

public class DataProvider : IDataProvider
{
    private readonly ILogger _log;

    private readonly IDataObjectProvider _dataObjectProvider;
    private readonly ICacheManager _cacheManager;

    public static readonly string IdentityRefFieldValueName = "Microsoft.VisualStudio.Services.WebApi.IdentityRef";
    public static readonly string SystemIdFieldName = "System.Id";
    public static readonly string WorkItemHtmlUrlFieldName = "DevHome.AzureExtension.WorkItemHtmlUrl";
    public static readonly string WorkItemTypeFieldName = "System.WorkItemType";

    public static readonly int PullRequestResultLimit = 25;

    public event CacheManagerUpdateEventHandler? OnUpdate;

    public DataProvider(IDataObjectProvider dataObjectProvider, ICacheManager cacheManager)
    {
        _log = Log.ForContext("SourceContext", nameof(IDataProvider));
        _cacheManager = cacheManager;
        _dataObjectProvider = dataObjectProvider;

        _cacheManager.OnUpdate += OnCacheManagerUpdate;
    }

    public void OnCacheManagerUpdate(object? source, CacheManagerUpdateEventArgs e)
    {
        OnUpdate?.Invoke(source, e);
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

    public async Task<IEnumerable<IPullRequest>> GetPullRequests(IPullRequestSearch pullRequestSearch)
    {

    }
}
