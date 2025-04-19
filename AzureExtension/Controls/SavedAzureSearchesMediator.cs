// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Helpers;

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
        if (AzureSearchHelper.IsIQuery(azureSearch))
        {
            QueryRemoved?.Invoke(this, azureSearch);
        }
        else if (AzureSearchHelper.IsIPullRequestSearch(azureSearch))
        {
            PullRequestSearchRemoved?.Invoke(this, azureSearch);
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
