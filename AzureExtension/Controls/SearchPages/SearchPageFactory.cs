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

    private readonly IAzureSearchRepository _azureSearchRepository;

    public SearchPageFactory(IResources resources, IDataProvider dataProvider, SavedAzureSearchesMediator mediator, IAccountProvider accountProvider, AzureClientHelpers azureClientHelpers, IAzureSearchRepository azureSearchRepository)
    {
        _resources = resources;
        _dataProvider = dataProvider;
        _mediator = mediator;
        _accountProvider = accountProvider;
        _azureClientHelpers = azureClientHelpers;
        _azureSearchRepository = azureSearchRepository;
    }

    public ListPage CreatePageForSearch(IAzureSearch search)
    {
        if (search is IQuery)
        {
            return new WorkItemsSearchPage((IQuery)search, _resources, _dataProvider);
        }
        else if (search is IPullRequestSearch)
        {
            return new PullRequestSearchPage((IPullRequestSearch)search, _resources, _dataProvider);
        }

        throw new NotImplementedException($"No page for search type {search.GetType()}");
    }

    public ContentPage CreateEditPageForSearch(IAzureSearch search)
    {
        if (search is IQuery)
        {
            var saveQueryForm = new SaveQueryForm((IQuery)search, _resources, _mediator, _accountProvider, _azureClientHelpers, (IQueryRepository)_azureSearchRepository);
            return new EditQueryPage(_resources, saveQueryForm, new StatusMessage(), "query edited successfully", "error in editing query");
        }
        else if (search is IPullRequestSearch)
        {
            var savePullRequestSearchForm = new SavePullRequestSearchForm((IPullRequestSearch)search, _resources, _mediator, _accountProvider, _azureClientHelpers, (ISavedPullRequestSearchRepository)_azureSearchRepository);
            return new EditPullRequestSearchPage(_resources, savePullRequestSearchForm, new StatusMessage(), "pull request search edited successfully", "error in editing pull request search");
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
            Icon = new IconInfo(AzureIcon.IconDictionary["logo"]),
            MoreCommands = new CommandContextItem[]
            {
                new(new LinkCommand(search.Url, _resources))
                {
                    Title = search.Name,
                    Icon = new IconInfo(AzureIcon.IconDictionary["logo"]),
                },
                new(CreateEditPageForSearch(search))
                {
                    Title = _resources.GetResource("Pages_Edit"),
                    Icon = new IconInfo("\uecc9"),
                },
                new(new RemoveAzureSearchCommand(search, _resources, _mediator, azureSearchRepository))
                {
                    Title = _resources.GetResource("Commands_Remove_Saved_Search"),
                    Icon = new IconInfo("\uecc9"),
                },
            },
        };
    }
}
