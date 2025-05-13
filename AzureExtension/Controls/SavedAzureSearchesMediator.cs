// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls.Pages;

namespace AzureExtension.Controls;

public class SavedAzureSearchesMediator
{
    public event EventHandler<SearchUpdatedEventArgs>? SearchRemoving;

    public event EventHandler<SearchUpdatedEventArgs>? SearchRemoved;

    public event EventHandler<SearchUpdatedEventArgs>? SearchSaved;

    private readonly SavedPipelineSearchesPage _savedPipelineSearchesPage;
    private readonly SavedPullRequestSearchesPage _savedPullRequestSearchesPage;
    private readonly SavedQueriesPage _savedQueriesPage;

    public SavedAzureSearchesMediator(SavedPipelineSearchesPage savedPipelineSearchesPage, SavedPullRequestSearchesPage savedPullRequestSearchesPage, SavedQueriesPage savedQueriesPage)
    {
        _savedPipelineSearchesPage = savedPipelineSearchesPage;
        _savedPullRequestSearchesPage = savedPullRequestSearchesPage;
        _savedQueriesPage = savedQueriesPage;
    }

    public void Remove(IAzureSearch azureSearch)
    {
        var args = new SearchUpdatedEventArgs(azureSearch);
        SearchRemoved?.Invoke(this, args);
        if (azureSearch is IPipelineDefinitionSearch)
        {
            _savedPipelineSearchesPage.OnPipelineSearchRemoved(this, args);
        }
        else if (azureSearch is IPullRequestSearch)
        {
            _savedPullRequestSearchesPage.OnPullRequestSearchRemoved(this, args);
        }
        else if (azureSearch is IQuerySearch)
        {
            _savedQueriesPage.OnQueryRemoved(this, args);
        }
    }

    public void RemovingSearch(IAzureSearch? search, Exception? ex = null)
    {
        var args = new SearchUpdatedEventArgs(search, ex);
        SearchRemoving?.Invoke(this, args);
        if (search is IPipelineDefinitionSearch)
        {
            _savedPipelineSearchesPage.OnPipelineSearchRemoving(this, args);
        }
        else if (search is IPullRequestSearch)
        {
            _savedPullRequestSearchesPage.OnPullRequestSearchRemoving(this, args);
        }
        else if (search is IQuerySearch)
        {
            _savedQueriesPage.OnQueryRemoving(this, args);
        }
    }

    public void AddSearch(IAzureSearch? search, Exception? ex = null)
    {
        var args = new SearchUpdatedEventArgs(search, ex);
        SearchSaved?.Invoke(this, args);
        if (search is IPullRequestSearch)
        {
            _savedPullRequestSearchesPage.OnPullRequestSearchSaved(this, args);
        }
        else if (search is IPipelineDefinitionSearch)
        {
            _savedPipelineSearchesPage.OnPipelineSearchSaved(this, args);
        }
        else if (search is IQuerySearch)
        {
            _savedQueriesPage.OnQuerySaved(this, args);
        }
    }
}
