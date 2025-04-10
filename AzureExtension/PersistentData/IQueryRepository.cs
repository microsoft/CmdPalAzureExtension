// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls;
using Microsoft.Identity.Client;

namespace AzureExtension.PersistentData;

public interface IQueryRepository
{
    Task AddSavedQueryAsync(IQuery query);

    Task RemoveSavedQueryAsync(IQuery query);

    public IQuery GetQuery(string name, string url);

    public Task<IEnumerable<IQuery>> GetSavedQueries();

    public Task<IEnumerable<IQuery>> GetTopLevelQueries();

    public Task<bool> IsTopLevel(IQuery query);

    public void UpdateQueryTopLevelStatus(IQuery query, bool isTopLevel, IAccount account);
}
