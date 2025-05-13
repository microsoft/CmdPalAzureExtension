// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls.Pages;

namespace AzureExtension.Controls;

public class SavedAzureSearchesMediator
{
    public event EventHandler<SearchUpdatedEventArgs>? SearchUpdated;

    public SavedAzureSearchesMediator()
    {
    }

    private SearchUpdatedType GetSearchType(IAzureSearch? search)
    {
        if (search is IQuerySearch)
        {
            return SearchUpdatedType.Query;
        }
        else if (search is IPullRequestSearch)
        {
            return SearchUpdatedType.PullRequest;
        }
        else if (search is IPipelineDefinitionSearch)
        {
            return SearchUpdatedType.Pipeline;
        }

        return SearchUpdatedType.Unknown;
    }

    public void Remove(IAzureSearch search)
    {
        var args = new SearchUpdatedEventArgs(search, SearchUpdatedEventType.SearchRemoved, GetSearchType(search));
        SearchUpdated?.Invoke(this, args);
    }

    public void RemovingSearch(IAzureSearch? search, Exception? ex = null)
    {
        var args = new SearchUpdatedEventArgs(search, SearchUpdatedEventType.SearchRemoving, GetSearchType(search));
        SearchUpdated?.Invoke(this, args);
    }

    public void AddSearch(IAzureSearch? search, Exception? ex = null)
    {
        var args = new SearchUpdatedEventArgs(search, SearchUpdatedEventType.SearchAdded, GetSearchType(search));
        SearchUpdated?.Invoke(this, args);
    }
}
