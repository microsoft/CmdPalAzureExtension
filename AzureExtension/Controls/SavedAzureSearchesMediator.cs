// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace AzureExtension.Controls;

public class SavedAzureSearchesMediator
{
    public event EventHandler<object?>? QueryRemoving;

    public event EventHandler<object?>? QueryRemoved;

    public event EventHandler<object?>? QuerySaved;

    public event EventHandler<object?>? PullRequestSearchSaved;

    public event EventHandler<object?>? PullRequestSearchRemoved;

    public event EventHandler<object?>? PullRequestSearchRemoving;

    public SavedAzureSearchesMediator()
    {
    }

    public void Remove(IAzureSearch azureSearch)
    {
        switch (azureSearch.Type)
        {
            case AzureSearchType.Query:
                QueryRemoved?.Invoke(this, azureSearch);
                break;
            case AzureSearchType.PullRequestSearch:
                PullRequestSearchRemoved?.Invoke(this, azureSearch);
                break;
            default:
                throw new InvalidOperationException($"Azure search type {azureSearch.Type} is not supported.");
        }
    }

    public void RemovingQuery(object args)
    {
        QueryRemoving?.Invoke(this, args);
    }

    public void AddQuery(object args)
    {
        QuerySaved?.Invoke(this, args);
    }

    public void AddPullRequestSearch(object args)
    {
        PullRequestSearchSaved?.Invoke(this, args);
    }

    public void RemovingPullRequestSearch(object args)
    {
        PullRequestSearchRemoving?.Invoke(this, args);
    }
}
