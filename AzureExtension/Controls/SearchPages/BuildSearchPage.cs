// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls.Commands;
using AzureExtension.DataManager.Cache;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Serilog;

namespace AzureExtension.Controls.Pages;

public partial class BuildSearchPage : ListPage
{
    protected ILogger Logger { get; }

    public ILiveDataProvider DataProvider { get; private set; }

    private readonly IPipelineDefinitionSearch _search;

    private readonly IDefinition _definition;

    private readonly IResources _resources;

    private readonly ILiveDataProvider _dataProvider;

    private readonly TimeSpanHelper _timeSpanHelper;

    public BuildSearchPage(IPipelineDefinitionSearch search, IResources resources, ILiveDataProvider dataProvider, TimeSpanHelper timeSpanHelper)
    {
        _search = search;
        _resources = resources;
        _dataProvider = dataProvider;
        _timeSpanHelper = timeSpanHelper;
        _definition = GetDefinitionForPage(_search).Result;
        Icon = GetIcon();
        Title = _definition.Name ?? $"{_resources.GetResource("Pages_BuildSearch_PipelineNameAlternative")} #{_definition.InternalId}";
        Name = Title; // Title is for the Page, Name is for the Command
        ShowDetails = true;
        Logger = Log.ForContext("SourceContext", $"Pages/{GetType().Name}");
        DataProvider = dataProvider;
    }

    private async Task<IDefinition> GetDefinitionForPage(IPipelineDefinitionSearch search)
    {
        var definition = await _dataProvider.GetSearchData<IDefinition>(search);
        if (definition == null)
        {
            throw new InvalidOperationException($"Definition not found for search {search.InternalId} - {search.ProjectUrl}");
        }

        return definition;
    }

    private IconInfo GetIcon()
    {
        var lastBuild = _definition.MostRecentBuild;
        if (lastBuild != null)
        {
            return IconLoader.GetIconForPipelineStatusAndResult(lastBuild.Status, lastBuild.Result);
        }

        return IconLoader.GetIcon("Logo");
    }

    protected void CacheManagerUpdateHandler(object? source, CacheManagerUpdateEventArgs e)
    {
        if (e.Kind == CacheManagerUpdateKind.Updated)
        {
            Logger.Information($"Received cache manager update event.");
            RaiseItemsChanged(0);
        }
    }

    public override IListItem[] GetItems() => DoGetItems(SearchText).GetAwaiter().GetResult();

    private async Task<IListItem[]> DoGetItems(string searchText)
    {
        try
        {
            var items = await GetSearchItemsAsync();
            if (items != null && items.Any())
            {
                var listItems = new List<IListItem>();
                foreach (var item in items)
                {
                    var listItem = GetListItem(item);
                    listItems.Add(listItem);
                }

                return listItems.ToArray();
            }
            else
            {
                return new IListItem[]
                {
                    new ListItem(new NoOpCommand())
                    {
                        Title = _resources.GetResource("Pages_BuildSearch_NoItemsMessage"),
                        Icon = IconLoader.GetIcon("Logo"),
                    },
                };
            }
        }
        catch (Exception ex)
        {
            return new ListItem[]
            {
                new(new NoOpCommand())
                {
                    Title = _resources.GetResource("Pages_BuildSearch_ErrorMessage"),
                    Details = new Details()
                    {
                        Body = ex.Message,
                    },
                    Icon = new IconInfo(string.Empty),
                },
            };
        }
    }

    private async Task<IEnumerable<IBuild>> GetSearchItemsAsync()
    {
        DataProvider.OnUpdate += CacheManagerUpdateHandler;

        var items = await LoadContentData();

        Logger.Information($"Found {items.Count()} items matching search query \"{_search.InternalId}\"");

        return items;
    }

    protected ListItem GetListItem(IBuild item)
    {
        var listItemTitle = $"#{item.BuildNumber} • {item.TriggerMessage}";

        return new ListItem(new LinkCommand(item.Url, _resources, null))
        {
            Title = listItemTitle,
            Icon = IconLoader.GetIconForPipelineStatusAndResult(item.Status, item.Result),
            Tags = new ITag[]
            {
                new Tag(_timeSpanHelper.DateTimeOffsetToDisplayString(new DateTime(item.StartTime), null)),
            },
            Details = new Details()
            {
                Title = $"{_definition.Name} - {listItemTitle}",
                Metadata = new[]
                {
                    new DetailsElement()
                    {
                        Key = _resources.GetResource("PipelineBuild_Requester"),
                        Data = new DetailsLink() { Text = $"{item.Requester?.Name}" },
                    },
                    new DetailsElement()
                    {
                        Key = _resources.GetResource("PipelineBuild_SourceBranch"),
                        Data = new DetailsLink() { Text = $"{item.SourceBranch}" },
                    },
                },
            },
        };
    }

    protected Task<IEnumerable<IBuild>> LoadContentData()
    {
        return _dataProvider.GetContentData<IBuild>(_search);
    }
}
