// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace AzureExtension.Controls;

public class SavedAzureSearchesMediator
{
    public event EventHandler<SearchUpdatedEventArgs>? SearchRemoving;

    public event EventHandler<SearchUpdatedEventArgs>? SearchRemoved;

    public event EventHandler<SearchUpdatedEventArgs>? SearchSaved;

    public class SearchUpdatedEventArgs : EventArgs
    {
        public IAzureSearch? AzureSearch { get; }

        public Exception? Exception { get; set; } = null!;

        public SearchUpdatedEventArgs(IAzureSearch? azureSearch, Exception? ex = null)
        {
            AzureSearch = azureSearch;
            Exception = ex;
        }
    }

    public SavedAzureSearchesMediator()
    {
    }

    public void Remove(IAzureSearch azureSearch)
    {
        SearchRemoved?.Invoke(this, new SearchUpdatedEventArgs(azureSearch));
    }

    public void RemovingSearch(IAzureSearch? search, Exception? ex = null)
    {
        SearchRemoving?.Invoke(this, new SearchUpdatedEventArgs(search, ex));
    }

    public void AddSearch(IAzureSearch? search, Exception? ex = null)
    {
        SearchSaved?.Invoke(this, new SearchUpdatedEventArgs(search, ex));
    }
}
