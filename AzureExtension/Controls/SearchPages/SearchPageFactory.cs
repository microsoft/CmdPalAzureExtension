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

    private readonly IDataProvider _dataProvider;

    private readonly SavedAzureSearchesMediator _mediator;

    private readonly IAccountProvider _accountProvider;

    private readonly AzureClientHelpers _azureClientHelpers;

    private readonly IPersistentDataRepository<IQuery> _queryRepository;

    private readonly IPersistentDataRepository<IPullRequestSearch> _savedPullRequestSearchRepository;

<<<<<<< HEAD
=======
    private readonly IDefinitionRepository _definitionRepository;

>>>>>>> main
    public SearchPageFactory(
        IResources resources,
        IDataProvider dataProvider,
        SavedAzureSearchesMediator mediator,
        IAccountProvider accountProvider,
        AzureClientHelpers azureClientHelpers,
<<<<<<< HEAD
        IPersistentDataRepository<IQuery> queryRepository,
        IPersistentDataRepository<IPullRequestSearch> savedPullRequestSearchRepository)
=======
        IQueryRepository queryRepository,
        ISavedPullRequestSearchRepository savedPullRequestSearchRepository,
        IDefinitionRepository definitionRepository)
>>>>>>> main
    {
        _resources = resources;
        _dataProvider = dataProvider;
        _mediator = mediator;
        _accountProvider = accountProvider;
        _azureClientHelpers = azureClientHelpers;
        _queryRepository = queryRepository;
        _savedPullRequestSearchRepository = savedPullRequestSearchRepository;
        _definitionRepository = definitionRepository;
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

        throw new NotImplementedException($"No page for search type {search.GetType()}");
    }

    public ListPage CreatePageForSearch(IDefinitionSearch search)
    {
        return new BuildSearchPage(search, _resources, _dataProvider, new TimeSpanHelper(_resources));
    }

    public ContentPage CreateEditPageForSearch(IAzureSearch search)
    {
        if (search is IQuery)
        {
            var saveQueryForm = new SaveQueryForm((IQuery)search, _resources, _mediator, _accountProvider, _azureClientHelpers, _queryRepository);
            var statusMessage = new StatusMessage();
            return new EditQueryPage(_resources, saveQueryForm, statusMessage, "Query edited successfully", "Error in editing query");
        }
        else if (search is IPullRequestSearch)
        {
            var savePullRequestSearchForm = new SavePullRequestSearchForm((IPullRequestSearch)search, _resources, _mediator, _accountProvider, _savedPullRequestSearchRepository);
            var statusMessage = new StatusMessage();
            return new EditPullRequestSearchPage(_resources, savePullRequestSearchForm, statusMessage, "Pull request search edited successfully", "error in editing pull request search");
        }
        else
        {
            throw new NotImplementedException($"No edit form for search type {search.GetType()}");
        }
    }

<<<<<<< HEAD
    public IListItem CreateItemForSearch(IAzureSearch search)
=======
    public ContentPage CreateEditPageForSearch(IDefinitionSearch search)
    {
        var savePipelineSearchForm = new SavePipelineSearchForm(search, _resources, _definitionRepository, _mediator, _accountProvider, _azureClientHelpers);
        var statusMessage = new StatusMessage();
        return new EditPipelineSearchPage(_resources, savePipelineSearchForm, statusMessage);
    }

    public IListItem CreateItemForSearch(IAzureSearch search, IAzureSearchRepository azureSearchRepository)
>>>>>>> main
    {
        IAzureSearchRepository azureSearchRepository = search is IQuery
            ? new AzureSearchRepositoryAdapter<IQuery>(_queryRepository)
            : new AzureSearchRepositoryAdapter<IPullRequestSearch>(_savedPullRequestSearchRepository);

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

<<<<<<< HEAD
    public Task<List<IListItem>> CreateCommandsForTopLevelSearches()
    {
        var topLevelSearches = new List<IListItem>();
        var topLevelQueries = _queryRepository.GetAllSavedData(true);
        var topLevelPullRequestSearches = _savedPullRequestSearchRepository.GetAllSavedData(true);
=======
    public IListItem CreateItemForSearch(IDefinitionSearch search, IDefinitionRepository definitionRepository)
    {
        var definition = _definitionRepository.GetDefinition(search, _accountProvider.GetDefaultAccount()).Result;
        var timeSpanHelper = new TimeSpanHelper(_resources);

        if (definition.MostRecentBuild != null)
        {
            return new ListItem(CreatePageForSearch(search))
            {
                MoreCommands = new CommandContextItem[]
                {
                    new(new LinkCommand(definition.HtmlUrl, _resources, _resources.GetResource("Pages_PipelineSearch_LinkCommandName"))),
                    new(CreateEditPageForSearch(search)),
                    new(new RemoveDefinitionSearchCommand(search, _resources, _mediator, definitionRepository)),
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
                    new(new RemoveDefinitionSearchCommand(search, _resources, _mediator, definitionRepository)),
                },
            };
        }
    }

    public async Task<List<IListItem>> CreateCommandsForTopLevelSearches()
    {
        var topLevelSearches = new List<IListItem>();
        var topLevelQueries = await _queryRepository.GetTopLevelQueries();
        var topLevelPullRequestSearches = await _savedPullRequestSearchRepository.GetTopLevelPullRequestSearches();
        var topLevelPipelineSearches = await _definitionRepository.GetAllDefinitionSearchesAsync(true);
>>>>>>> main

        foreach (var query in topLevelQueries)
        {
            var commandItem = CreateItemForSearch(query);
            topLevelSearches.Add(commandItem);
        }

        foreach (var pullRequestSearch in topLevelPullRequestSearches)
        {
            var commandItem = CreateItemForSearch(pullRequestSearch);
            topLevelSearches.Add(commandItem);
        }

<<<<<<< HEAD
        return Task.FromResult(topLevelSearches);
=======
        foreach (var pipelineSearch in topLevelPipelineSearches)
        {
            var commandItem = CreateItemForSearch(pipelineSearch, _definitionRepository);
            topLevelSearches.Add(commandItem);
        }

        return topLevelSearches;
>>>>>>> main
    }
}
