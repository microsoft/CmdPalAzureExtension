// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace AzureExtension.Controls;

public class SavedQueriesMediator
{
    public event EventHandler<object?>? QueryRemoving;

    public event EventHandler<object?>? QueryRemoved;

    public event EventHandler<object?>? QuerySaved;

    public SavedQueriesMediator()
    {
    }

    public void RemovingQuery(object args)
    {
        QueryRemoving?.Invoke(this, args);
    }

    public void RemoveQuery(object args)
    {
        QueryRemoved?.Invoke(this, args);
    }

    public void AddQuery(object args)
    {
        QuerySaved?.Invoke(this, args);
    }
}
