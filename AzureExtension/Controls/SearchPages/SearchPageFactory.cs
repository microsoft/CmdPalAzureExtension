// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Account;
using AzureExtension.Client;
using AzureExtension.Controls.Commands;
using AzureExtension.Controls.Forms;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Pages;

public class SearchPageFactory : ISearchPageFactory
{
    private readonly IResources _resources;
    private readonly SavedAzureSearchesMediator _mediator;
    private readonly IAccountProvider _accountProvider;
    private readonly AzureClientHelpers _azureClientHelpers;
    private readonly ISavedSearchesUpdater<IQuerySearch> _queryUpdater;
    private readonly ISavedSearchesUpdater<IPullRequestSearch> _savedPullRequestSearchUpdater;
    private readonly ISavedSearchesUpdater<IPipelineDefinitionSearch> _definitionUpdater;
    private readonly ILiveContentDataProvider<IWorkItem> _workItemProvider;
    private readonly ILiveContentDataProvider<IPullRequest> _pullRequestProvider;
    private readonly ILiveContentDataProvider<IBuild> _buildProvider;
    private readonly ILiveSearchDataProvider<IDefinition> _definitionProvider;
    private readonly IDictionary<Type, IAzureSearchRepository> _azureSearchRepositories;

    public SearchPageFactory(
        IResources resources,
        SavedAzureSearchesMediator mediator,
        IAccountProvider accountProvider,
        AzureClientHelpers azureClientHelpers,
        IDictionary<Type, IAzureSearchRepository> azureSearchRepositories,
        ISavedSearchesUpdater<IQuerySearch> queryUpdater,
        ISavedSearchesUpdater<IPullRequestSearch> savedPullRequestSearchUpdater,
        ISavedSearchesUpdater<IPipelineDefinitionSearch> definitionUpdater,
        ILiveContentDataProvider<IWorkItem> workItemProvider,
        ILiveContentDataProvider<IPullRequest> pullRequestProvider,
        ILiveContentDataProvider<IBuild> buildProvider,
        ILiveSearchDataProvider<IDefinition> definitionProvider,
        SaveSearchCommand<IQuerySearch> saveQuerySearchCommand,
        SaveSearchCommand<IPullRequestSearch> savePullRequestSearchCommand,
        SaveSearchCommand<IPipelineDefinitionSearch> savePipelineSearchCommand)
    {
        _resources = resources;
        _mediator = mediator;
        _accountProvider = accountProvider;
        _azureClientHelpers = azureClientHelpers;
        _queryUpdater = queryUpdater;
        _savedPullRequestSearchUpdater = savedPullRequestSearchUpdater;
        _definitionUpdater = definitionUpdater;
        _azureSearchRepositories = azureSearchRepositories;
        _workItemProvider = workItemProvider;
        _pullRequestProvider = pullRequestProvider;
        _buildProvider = buildProvider;
        _definitionProvider = definitionProvider;
    }

    public ListPage CreatePageForSearch(IAzureSearch search)
    {
        if (search is IQuerySearch)
        {
            return new WorkItemsSearchPage((IQuerySearch)search, _resources, _workItemProvider, new TimeSpanHelper(_resources));
        }
        else if (search is IPullRequestSearch)
        {
            return new PullRequestSearchPage((IPullRequestSearch)search, _resources, _pullRequestProvider, new TimeSpanHelper(_resources));
        }
        else if (search is IPipelineDefinitionSearch)
        {
            return new BuildSearchPage((IPipelineDefinitionSearch)search, _resources, _buildProvider, _definitionProvider, new TimeSpanHelper(_resources));
        }

        throw new NotImplementedException($"No page for search type {search.GetType()}");
    }

    public ContentPage CreateEditPageForSearch(IAzureSearch search)
    {
        if (search is IQuerySearch)
        {
            var saveQuerySearchCommand = new SaveSearchCommand<IQuerySearch>(_queryUpdater, _mediator, (IQuerySearch)search, _resources.GetResource("Message_Query_Saved"), _resources.GetResource("Message_Query_Saved_Error"), _resources.GetResource("Pages_EditQuery_SuccessMessage"), _resources.GetResource("Pages_EditQuery_FailureMessage"));
            var saveQueryForm = new SaveQueryForm((IQuerySearch)search, _resources, _mediator, _accountProvider, _azureClientHelpers, _queryUpdater, saveQuerySearchCommand);
            return new SaveQueryPage(saveQueryForm, _resources, _mediator);
        }
        else if (search is IPullRequestSearch)
        {
            var savePullRequestSearchCommand = new SaveSearchCommand<IPullRequestSearch>(_savedPullRequestSearchUpdater, _mediator, (IPullRequestSearch)search, _resources.GetResource("Messages_PullRequestSearch_Saved"), _resources.GetResource("Pages_SavePullRequestSearch_FailureMessage"), _resources.GetResource("Pages_EditPullRequestSearch_SuccessMessage"), _resources.GetResource("Pages_EditPullRequestSearch_FailureMessage"));
            var savePullRequestSearchForm = new SavePullRequestSearchForm((IPullRequestSearch)search, _resources, _mediator, _accountProvider, _azureClientHelpers, _savedPullRequestSearchUpdater, savePullRequestSearchCommand);
            return new SavePullRequestSearchPage(savePullRequestSearchForm, _resources, _mediator);
        }
        else if (search is IPipelineDefinitionSearch)
        {
            var savePipelineSearchCommand = new SaveSearchCommand<IPipelineDefinitionSearch>(_definitionUpdater, _mediator, (IPipelineDefinitionSearch)search, _resources.GetResource("Pages_SavePipelineSearch_SuccessMessage"), _resources.GetResource("Pages_SavePipelineSearch_FailureMessage"), _resources.GetResource("Pages_EditPipelineSearch_SuccessMessage"), _resources.GetResource("Pages_EditPipelineSearch_FailureMessage"));
            var savePipelineSearchForm = new SavePipelineSearchForm((IPipelineDefinitionSearch)search, _resources, _definitionUpdater, _mediator, _accountProvider, _azureClientHelpers, savePipelineSearchCommand);
            return new SavePipelineSearchPage(savePipelineSearchForm, _resources, _mediator);
        }
        else
        {
            throw new NotImplementedException($"No edit form for search type {search.GetType()}");
        }
    }

    public IListItem CreateItemForSearch(IAzureSearch search)
    {
        if (search is IPipelineDefinitionSearch)
        {
            return CreateItemForDefinitionSearch((IPipelineDefinitionSearch)search);
        }

        IAzureSearchRepository azureSearchRepository = _azureSearchRepositories[SearchHelper.GetAzureSearchType(search)];
        var removeCommandSuccessMessage = search is IQuerySearch
            ? _resources.GetResource("Message_Query_Removed")
            : search is IPullRequestSearch
                ? _resources.GetResource("Message_PullRequestSearch_Removed")
                : string.Empty;
        var removeCommandFailureMessage = search is IQuerySearch
            ? _resources.GetResource("Messages_RemoveQuery_Failure")
            : search is IPullRequestSearch
                ? _resources.GetResource("Messages_RemovePullRequestSearch_Failure")
                : string.Empty;

        return new ListItem(CreatePageForSearch(search))
        {
            Title = search.Name,
            Subtitle = search.Url,
            Icon = search is IQuerySearch ? IconLoader.GetIcon("Query") : IconLoader.GetIcon("PullRequest"),
            MoreCommands = new CommandContextItem[]
            {
                new(new LinkCommand(search.Url, _resources, null)),
                new(CreateEditPageForSearch(search)),
                new(new RemoveCommand(
                    search,
                    _resources,
                    _mediator,
                    azureSearchRepository,
                    removeCommandSuccessMessage,
                    removeCommandFailureMessage,
                    _azureClientHelpers,
                    _accountProvider)),
            },
        };
    }

    public IListItem CreateItemForDefinitionSearch(IPipelineDefinitionSearch search)
    {
        var definition = _definitionProvider.GetSearchData(search).Result;
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
                    new(new RemoveCommand(
                        search,
                        _resources,
                        _mediator,
                        azureSearchRepository,
                        _resources.GetResource("Message_RemovePipeline_Success"),
                        _resources.GetResource("Messages_RemovePipeline_Failure"),
                        _azureClientHelpers,
                        _accountProvider)),
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
                    new(new RemoveCommand(
                        search,
                        _resources,
                        _mediator,
                        azureSearchRepository,
                        _resources.GetResource("Message_RemovePipeline_Success"),
                        _resources.GetResource("Messages_RemovePipeline_Failure"),
                        _azureClientHelpers,
                        _accountProvider)),
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
