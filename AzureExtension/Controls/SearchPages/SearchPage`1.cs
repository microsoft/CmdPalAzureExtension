// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Dynamic;
using System.Text.Json;
using AzureExtension.Client;
using AzureExtension.DataModel;
using AzureExtension.DeveloperId;
using AzureExtension.Helpers;
using Microsoft.Azure.Pipelines.WebApi;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using Serilog;
using TFModels = Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace AzureExtension.Controls.Pages;

public class SearchPage<T> : ListPage
{
    private readonly ILogger _log;

    public ISearch CurrentSearch { get; private set; }

    protected IResources Resources { get; private set; }

    private IDeveloperIdProvider DeveloperIdProvider { get; set; }

    private AzureDataManager AzureDataManager { get; set; }

    private IDeveloperId _developerId;

    // Max number of query results to fetch for a given query.
    public static readonly int QueryResultLimit = 25;

    // Connections are a pairing of DeveloperId and a Uri.
    private static readonly ConcurrentDictionary<Tuple<Uri, IDeveloperId>, VssConnection> _connections = new();

    // Search is mandatory for this page to exist
    public SearchPage(ISearch search, IResources resources, IDeveloperIdProvider developerIdProvider, AzureDataManager azureDataManager)
    {
        Icon = new IconInfo(AzureIcon.IconDictionary["logo"]);
        Name = search.Name;
        CurrentSearch = search;
        Resources = resources;
        _log = Serilog.Log.ForContext("SourceContext", $"AzureExtension/Controls/Pages/{nameof(SearchPage<T>)}");
        DeveloperIdProvider = developerIdProvider;
        _developerId = developerIdProvider.GetLoggedInDeveloperIdsInternal().FirstOrDefault() ?? throw new ArgumentNullException(nameof(developerIdProvider));
        AzureDataManager = azureDataManager;
    }

    public override IListItem[] GetItems() => DoGetItems(SearchText).GetAwaiter().GetResult();

    private async Task<IListItem[]> DoGetItems(string query)
    {
        try
        {
            _log.Information($"Getting items for search query \"{CurrentSearch.Name}\"");
            var items = await GetSearchItems();

            var iconString = "logo";

            if (items.Any())
            {
                return items.Select(item => GetListItem(item!)).ToArray();
            }
            else
            {
                return !items.Any()
                    ? new ListItem[]
                    {
                            new(new NoOpCommand())
                            {
                                Title = Resources.GetResource("Pages_No_Items_Found"),
                                Icon = new IconInfo(AzureIcon.IconDictionary[iconString]),
                            },
                    }
                    :
                    [
                            new ListItem(new NoOpCommand())
                            {
                                Title = Resources.GetResource("Pages_Error_Title"),
                                Details = new Details()
                                {
                                    Body = Resources.GetResource("Pages_Error_Body"),
                                },
                                Icon = new IconInfo(AzureIcon.IconDictionary[iconString]),
                            },
                    ];
            }
        }
        catch (Exception ex)
        {
            return
            [
                    new ListItem(new NoOpCommand())
                    {
                        Title = Resources.GetResource("Pages_Error_Title"),
                        Details = new Details()
                        {
                            Title = ex.Message,
                            Body = string.IsNullOrEmpty(ex.StackTrace) ? "There is no stack trace for the error." : ex.StackTrace,
                        },
                    },
            ];
        }
    }

    public ListItem GetListItem(TFModels.WorkItem item) => new ListItem(new NoOpCommand())
    {
        Title = item.Fields["System.Title"].ToString() ?? item.Fields["System.Id"].ToString() ?? string.Empty,
        Icon = new IconInfo(AzureIcon.IconDictionary["logo"]),
    };

    private async Task<IEnumerable<TFModels.WorkItem>> GetSearchItems()
    {
        return await AzureDataManager.GetWorkItemsAsync(CurrentSearch.Uri!, _developerId);
    }
}
