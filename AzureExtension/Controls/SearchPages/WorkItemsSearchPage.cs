// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls.Commands;
using AzureExtension.Controls.SearchPages;
using AzureExtension.DataManager;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Serilog;

namespace AzureExtension.Controls.Pages;

public sealed partial class WorkItemsSearchPage : SearchPage<IWorkItem>
{
    // Max number of query results to fetch for a given query.
    public static readonly int QueryResultLimit = 25;

    private readonly Lazy<ILogger> _log = new(() => Serilog.Log.ForContext("SourceContext", $"Pages/WorkItemsSearchPage"));

    private ILogger Log => _log.Value;

    private readonly IQuery _query;

    private readonly IResources _resources;

    private readonly IDataProvider _dataProvider;

    private readonly TimeSpanHelper _timeSpanHelper;

    public WorkItemsSearchPage(IQuery query, IResources resources, IDataProvider dataProvider, TimeSpanHelper timeSpanHelper)
        : base(query)
    {
        _query = query;
        _resources = resources;
        _dataProvider = dataProvider;
        Icon = new IconInfo(AzureIcon.IconDictionary["logo"]);
        Name = query.Name;
        _timeSpanHelper = timeSpanHelper;
    }

    protected override ListItem GetListItem(IWorkItem item)
    {
        var title = item.SystemTitle;
        var url = item.HtmlUrl;

        return new ListItem(new LinkCommand(url, _resources))
        {
            Title = title,
            Icon = new IconInfo(AzureIcon.IconDictionary["logo"]),
        };
    }

    protected override Task<IEnumerable<IWorkItem>> LoadContentData()
    {
        return _dataProvider.GetWorkItems(_query);
    }
}
