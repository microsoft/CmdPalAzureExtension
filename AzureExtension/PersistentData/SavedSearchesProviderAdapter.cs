// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls;
using AzureExtension.DataManager;

namespace AzureExtension.PersistentData;

#pragma warning disable SA1649 // File name should match first type name
public class SavedSearchesProviderAdapter<TDataSearch> : ISavedSearchesSource<TDataSearch>
#pragma warning restore SA1649 // File name should match first type name
    where TDataSearch : IAzureSearch
{
    private readonly ISavedSearchesProvider<TDataSearch> _repository;

    public SavedSearchesProviderAdapter(IPersistentSearchRepository<TDataSearch> repository)
    {
        _repository = repository;
    }

    public IEnumerable<TDataSearch> GetSavedSearches()
    {
        return _repository.GetSavedSearches();
    }
}
