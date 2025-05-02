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

    private readonly IQueryRepository _queryRepository;

    private readonly ISavedPullRequestSearchRepository _savedPullRequestSearchRepository;

    public SearchPageFactory(IResources resources, IDataProvider dataProvider, SavedAzureSearchesMediator mediator, IAccountProvider accountProvider, AzureClientHelpers azureClientHelpers, IQueryRepository queryRepository, ISavedPullRequestSearchRepository savedPullRequestSearchRepository)
    {
        _resources = resources;
        _dataProvider = dataProvider;
        _mediator = mediator;
        _accountProvider = accountProvider;
        _azureClientHelpers = azureClientHelpers;
        _queryRepository = queryRepository;
        _savedPullRequestSearchRepository = savedPullRequestSearchRepository;
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

    public IListItem CreateItemForSearch(IAzureSearch search, IAzureSearchRepository azureSearchRepository)
    {
        return new ListItem(CreatePageForSearch(search))
        {
            Title = search.Name,
            Subtitle = search.Url,
            Icon = search is IQuery ? IconLoader.GetIcon("Query") : IconLoader.GetIcon("PullRequest"),
            MoreCommands = new CommandContextItem[]
            {
                new(new LinkCommand(search.Url, _resources)),
                new(CreateEditPageForSearch(search)),
                new(new RemoveAzureSearchCommand(search, _resources, _mediator, azureSearchRepository)),
            },
        };
    }

    public async Task<List<IListItem>> CreateCommandsForTopLevelSearches()
    {
        var topLevelSearches = new List<IListItem>();
        var topLevelQueries = await _queryRepository.GetTopLevelQueries();
        var topLevelPullRequestSearches = await _savedPullRequestSearchRepository.GetTopLevelPullRequestSearches();

        foreach (var query in topLevelQueries)
        {
            var commandItem = CreateItemForSearch(query, _queryRepository);
            topLevelSearches.Add(commandItem);
        }

        foreach (var pullRequestSearch in topLevelPullRequestSearches)
        {
            var commandItem = CreateItemForSearch(pullRequestSearch, _savedPullRequestSearchRepository);
            topLevelSearches.Add(commandItem);
        }

        return topLevelSearches;
    }
}
