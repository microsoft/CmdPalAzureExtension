// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls;
using Microsoft.Identity.Client;

namespace AzureExtension.PersistentData;

public interface IQueryRepository : IAzureSearchRepository
{
    Task AddSavedQueryAsync(IQuery query);

    Task RemoveSavedQueryAsync(IQuery query);

    IQuery GetQuery(string name, string url);

    Task<IEnumerable<IQuery>> GetSavedQueries();

    Task<IEnumerable<IQuery>> GetTopLevelQueries();

    Task<bool> IsTopLevel(IQuery query);

    void UpdateQueryTopLevelStatus(IQuery query, bool isTopLevel, IAccount account);
}
