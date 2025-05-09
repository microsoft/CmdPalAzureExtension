// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Account;
using AzureExtension.Client;
using AzureExtension.Controls.Commands;
using AzureExtension.Controls.Forms;
using AzureExtension.Helpers;
using AzureExtension.PersistentData;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Pages;

public class SearchPageFactory : ISearchPageFactory
{
    private readonly IResources _resources;

    private readonly ILiveDataProvider _dataProvider;

    private readonly SavedAzureSearchesMediator _mediator;

    private readonly IAccountProvider _accountProvider;

    private readonly AzureClientHelpers _azureClientHelpers;

    private readonly ISavedSearchesUpdater<IQuery> _queryUpdater;

    private readonly ISavedSearchesUpdater<IPullRequestSearch> _savedPullRequestSearchUpdater;

    private readonly ISavedSearchesUpdater<IPipelineDefinitionSearch> _definitionUpdater;

    private readonly IDictionary<Type, IAzureSearchRepository> _azureSearchRepositories;

    public SearchPageFactory(
        IResources resources,
        ILiveDataProvider dataProvider,
        SavedAzureSearchesMediator mediator,
        IAccountProvider accountProvider,
        AzureClientHelpers azureClientHelpers,
        IDictionary<Type, IAzureSearchRepository> azureSearchRepositories,
        ISavedSearchesUpdater<IQuery> queryUpdater,
        ISavedSearchesUpdater<IPullRequestSearch> savedPullRequestSearchUpdater,
        ISavedSearchesUpdater<IPipelineDefinitionSearch> definitionUpdater)
    {
        _resources = resources;
        _dataProvider = dataProvider;
        _mediator = mediator;
        _accountProvider = accountProvider;
        _azureClientHelpers = azureClientHelpers;
        _queryUpdater = queryUpdater;
        _savedPullRequestSearchUpdater = savedPullRequestSearchUpdater;
        _definitionUpdater = definitionUpdater;
        _azureSearchRepositories = azureSearchRepositories;
    }

    public ListPage CreatePageForSearch(IAzureSearch search)
    {
        if (search is IQuery)
        {
            return new WorkItemsSearchPage((IQuery)search, _resources, _dataProvider, new TimeSpanHelper(_resources));
        }
        else if (search is IPullRequestSearch)
        {
            return new PullRequestSearchPage((IPullRequestSearch)search, _resources, _dataProvider, new TimeSpanHelper(_resources));
        }
        else if (search is IPipelineDefinitionSearch)
        {
            return new BuildSearchPage((IPipelineDefinitionSearch)search, _resources, _dataProvider, new TimeSpanHelper(_resources));
        }

        throw new NotImplementedException($"No page for search type {search.GetType()}");
    }

    public ContentPage CreateEditPageForSearch(IAzureSearch search)
    {
        if (search is IQuery)
        {
            var saveQueryForm = new SaveQueryForm((IQuery)search, _resources, _mediator, _accountProvider, _azureClientHelpers, _queryUpdater);
            var statusMessage = new StatusMessage();
            return new EditQueryPage(_resources, saveQueryForm, statusMessage, "Query edited successfully", "Error in editing query");
        }
        else if (search is IPullRequestSearch)
        {
            var savePullRequestSearchForm = new SavePullRequestSearchForm((IPullRequestSearch)search, _resources, _mediator, _accountProvider, _savedPullRequestSearchUpdater);
            var statusMessage = new StatusMessage();
            return new EditPullRequestSearchPage(_resources, savePullRequestSearchForm, statusMessage, "Pull request search edited successfully", "error in editing pull request search");
        }
        else if (search is IPipelineDefinitionSearch)
        {
            var savePipelineSearchForm = new SavePipelineSearchForm((IPipelineDefinitionSearch)search, _resources, _definitionUpdater, _mediator, _accountProvider, _azureClientHelpers);
            var statusMessage = new StatusMessage();
            return new EditPipelineSearchPage(_resources, savePipelineSearchForm, statusMessage);
        }
        else
        {
            throw new NotImplementedException($"No edit form for search type {search.GetType()}");
        }
    }

    private Type GetAzureSearchType(IAzureSearch search)
    {
        if (search is IQuery)
        {
            return typeof(IQuery);
        }
        else if (search is IPullRequestSearch)
        {
            return typeof(IPullRequestSearch);
        }
        else if (search is IPipelineDefinitionSearch)
        {
            return typeof(IPipelineDefinitionSearch);
        }

        throw new NotImplementedException($"No type for search {search.GetType()}");
    }

    public IListItem CreateItemForSearch(IAzureSearch search)
    {
        if (search is IPipelineDefinitionSearch)
        {
            return CreateItemForDefinitionSearch((IPipelineDefinitionSearch)search);
        }

        IAzureSearchRepository azureSearchRepository = _azureSearchRepositories[GetAzureSearchType(search)];

        return new ListItem(CreatePageForSearch(search))
        {
            Title = search.Name,
            Subtitle = search.Url,
            Icon = search is IQuery ? IconLoader.GetIcon("Query") : IconLoader.GetIcon("PullRequest"),
            MoreCommands = new CommandContextItem[]
            {
                new(new LinkCommand(search is IQuery ? search.Url : $"{search.Url}/pullrequests", _resources, null)),
                new(CreateEditPageForSearch(search)),
                new(new RemoveCommand(search, _resources, _mediator, azureSearchRepository)),
            },
        };
    }

    public IListItem CreateItemForDefinitionSearch(IPipelineDefinitionSearch search)
    {
        var definition = _dataProvider.GetDefinition(search).Result;
        var timeSpanHelper = new TimeSpanHelper(_resources);

        var azureSearchRepository = _azureSearchRepositories[typeof(IPipelineDefinitionSearch)];

        if (definition.MostRecentBuild != null)
        {
            return new ListItem(CreatePageForSearch(search))
            {
                MoreCommands = new CommandContextItem[]
                {
                    new(new LinkCommand(definition.HtmlUrl, _resources, _resources.GetResource("Pages_PipelineSearch_LinkCommandName"))),
                    new(CreateEditPageForSearch(search)),
                    new(new RemoveCommand(search, _resources, _mediator, azureSearchRepository)),
                },
                Tags = new ITag[]
                {
                    new Tag(timeSpanHelper.DateTimeOffsetToDisplayString(new DateTime(definition.MostRecentBuild!.StartTime), null)),
                },
                Details = new Details()
                {
                    Title = $"{definition.Name} - {definition.MostRecentBuild!.BuildNumber}",
                    Metadata = new[]
                    {
                        new DetailsElement()
                        {
                            Key = _resources.GetResource("PipelineBuild_Requester"),
                            Data = new DetailsLink() { Text = $"{definition.MostRecentBuild!.Requester?.Name}" },
                        },
                        new DetailsElement()
                        {
                            Key = _resources.GetResource("PipelineBuild_SourceBranch"),
                            Data = new DetailsLink() { Text = $"{definition.MostRecentBuild!.SourceBranch}" },
                        },
                    },
                },
            };
        }
        else
        {
            return new ListItem(CreatePageForSearch(search))
            {
                Title = definition.Name,
                Icon = IconLoader.GetIcon("Pipeline"),
                MoreCommands = new CommandContextItem[]
                {
                    new(new LinkCommand(definition.HtmlUrl, _resources, _resources.GetResource("Pages_PipelineSearch_LinkCommandName"))),
                    new(CreateEditPageForSearch(search)),
                    new(new RemoveCommand(search, _resources, _mediator, azureSearchRepository)),
                },
            };
        }
    }

    public Task<List<IListItem>> CreateCommandsForTopLevelSearches()
    {
        var topLevelSearches = new List<IListItem>();

        foreach (var azureSearchRepository in _azureSearchRepositories.Values)
        {
            var searches = azureSearchRepository.GetAll(true);
            foreach (var search in searches)
            {
                var commandItem = CreateItemForSearch(search);
                topLevelSearches.Add(commandItem);
            }
        }

        return Task.FromResult(topLevelSearches);
    }
}
