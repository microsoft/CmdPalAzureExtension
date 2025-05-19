// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls.Commands;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Pages;

public partial class BuildSearchPage : SearchPage<IBuild>
{
    private readonly IPipelineDefinitionSearch _search;
    private readonly IDefinition _definition;
    private readonly IResources _resources;
    private readonly ILiveSearchDataProvider<IDefinition> _searchDataProvider;
    private readonly TimeSpanHelper _timeSpanHelper;

    public BuildSearchPage(
        IPipelineDefinitionSearch search,
        IResources resources,
        ILiveContentDataProvider<IBuild> contentDataProvider,
        ILiveSearchDataProvider<IDefinition> searchDataProvider,
        TimeSpanHelper timeSpanHelper)
        : base(search, contentDataProvider, resources)
    {
        _search = search;
        _resources = resources;
        _searchDataProvider = searchDataProvider;
        _timeSpanHelper = timeSpanHelper;
        _definition = GetDefinitionForPage(_search).Result;
        Icon = GetIcon();
        Title = _definition.Name ?? $"{_resources.GetResource("Pages_BuildSearch_PipelineNameAlternative")} #{_definition.InternalId}";
        Name = Title; // Title is for the Page, Name is for the Command
        ShowDetails = true;
    }

    private async Task<IDefinition> GetDefinitionForPage(IPipelineDefinitionSearch search)
    {
        var definition = await _searchDataProvider.GetSearchData(search);
        if (definition == null)
        {
            throw new InvalidOperationException($"Definition not found for search {search.InternalId} - {search.Url}");
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

        return IconLoader.GetIcon("Pipeline");
    }

    protected override ListItem GetListItem(IBuild item)
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
}
