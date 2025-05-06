// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls.Commands;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Pages;

public sealed partial class PipelineSearchPage : SearchPage<IDefinition>
{
    private readonly IDefinitionSearch _search;

    private readonly IResources _resources;

    private readonly IDataProvider _dataProvider;

    private readonly TimeSpanHelper _timeSpanHelper;

    public PipelineSearchPage(IDefinitionSearch search, IResources resources, IDataProvider dataProvider, TimeSpanHelper timeSpanHelper)
        : base(search, dataProvider)
    {
        _search = search;
        _resources = resources;
        _dataProvider = dataProvider;
        _timeSpanHelper = timeSpanHelper;
        Icon = IconLoader.GetIcon("Pipeline");
        Name = search.Name;
        ShowDetails = true;
    }

    protected override ListItem GetListItem(IDefinition item)
    {
        var title = item.Name;
        var url = item.InternalId.ToStringInvariant();

        return new ListItem(new LinkCommand(url, _resources))
        {
            Title = title,
            Icon = IconLoader.GetIcon("Logo"),
        };
    }

    protected override Task<IEnumerable<IDefinition>> LoadContentData()
    {
        return (Task<IEnumerable<IDefinition>>)_dataProvider.GetDefinition(_search).Result;
    }
}
