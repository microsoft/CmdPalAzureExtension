// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Helpers;

namespace AzureExtension.Controls;

public class SavedAzureSearchesMediator
{
    public event EventHandler<SearchUpdatedEventArgs>? SearchUpdated;

    public event EventHandler<SearchSetLoadingStateArgs>? LoadingStateChanged;

    public SavedAzureSearchesMediator()
    {
    }

    public void Remove(IAzureSearch search)
    {
        var args = new SearchUpdatedEventArgs(search, SearchUpdatedEventType.SearchRemoved, SearchHelper.GetSearchUpdatedType(search));
        SearchUpdated?.Invoke(this, args);
    }

    public void AddSearch(IAzureSearch? search, Exception? ex = null)
    {
        var args = new SearchUpdatedEventArgs(search, SearchUpdatedEventType.SearchAdded, SearchHelper.GetSearchUpdatedType(search));
        SearchUpdated?.Invoke(this, args);
    }

    public void SetLoadingState(bool isLoading, SearchUpdatedType searchType)
    {
        var args = new SearchSetLoadingStateArgs(isLoading, searchType);
        LoadingStateChanged?.Invoke(this, args);
    }
}
