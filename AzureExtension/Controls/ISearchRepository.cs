// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.DeveloperId;

namespace AzureExtension.Controls;

public interface ISearchRepository
{
    ISearch GetSearch(string name, string searchString);

    Task<IEnumerable<ISearch>> GetSavedSearches();

    Task RemoveSavedSearch(ISearch search);

    bool ValidateSearch(ISearch search, IDeveloperId developerId);

    Task InitializeTopLevelSearches(IEnumerable<ISearch> searches);

    Task<IEnumerable<ISearch>> GetTopLevelSearches();

    Task<bool> IsTopLevel(ISearch search);

    void UpdateSearchTopLevelStatus(ISearch search, bool isTopLevel, IDeveloperId developerId);
}
