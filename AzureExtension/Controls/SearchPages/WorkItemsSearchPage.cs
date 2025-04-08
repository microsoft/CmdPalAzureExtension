// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using AzureExtension.DeveloperId;
using AzureExtension.Helpers;
using AzureExtension.PersistentData;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.VisualStudio.Services.WebApi;
using Serilog;
using TFModels = Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace AzureExtension.Controls.Pages;

public sealed partial class WorkItemsSearchPage(ISearch search, AzureDataManager azureDataManager, PersistentDataManager persistentDataManager, IResources resources, IDeveloperId developerId)
    : SearchPage<IWorkItem>(search, azureDataManager, persistentDataManager, resources, developerId)
{
    // Max number of query results to fetch for a given query.
    public static readonly int QueryResultLimit = 25;

    // Connections are a pairing of DeveloperId and a Uri.
    private static readonly ConcurrentDictionary<Tuple<Uri, IDeveloperId>, VssConnection> _connections = new();

    public override IListItem[] GetItems() => DoGetItems(SearchText).GetAwaiter().GetResult();

    private async Task<IListItem[]> DoGetItems(string query)
    {
        try
        {
            Log.Information($"Getting items for search query \"{CurrentSearch.Name}\"");
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
        return await AzureDataManager.GetWorkItemsAsync(CurrentSearch.Uri!, DeveloperId);
    }

    protected override ListItem GetListItem(IWorkItem item)
    {
        throw new NotImplementedException();
    }

    protected override Task<IEnumerable<IWorkItem>> LoadContentData()
    {
        throw new NotImplementedException();
    }
}
