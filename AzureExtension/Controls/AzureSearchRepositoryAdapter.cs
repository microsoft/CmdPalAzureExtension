// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.PersistentData;

namespace AzureExtension.Controls;

#pragma warning disable SA1649 // File name should match first type name
public class AzureSearchRepositoryAdapter<TDataSearch, TDataResult> : IAzureSearchRepository
#pragma warning restore SA1649 // File name should match first type name
    where TDataSearch : IAzureSearch
{
    private readonly ISavedSearchesUpdater<TDataSearch> _updater;
    private readonly ISavedSearchesProvider<TDataSearch> _provider;

    public AzureSearchRepositoryAdapter(IPersistentDataRepository<TDataSearch> repository)
    {
        _provider = repository;
        _updater = repository;
    }

    public AzureSearchRepositoryAdapter(IPersistentDataRepository<TDataSearch, TDataResult> repository)
    {
        _provider = repository;
        _updater = repository;
    }

    public IEnumerable<IAzureSearch> GetAll(bool getTopLevelOnly = false)
    {
        return _provider.GetSavedSearches(getTopLevelOnly)
            .Select(data => data as IAzureSearch)
            .Where(data => data != null);
    }

    public void Remove(IAzureSearch azureSearch)
    {
        _updater.RemoveSavedSearch((TDataSearch)azureSearch);
    }
}

#pragma warning disable SA1402 // File may only contain a single type
public class AzureSearchRepositoryAdapter<TDataSearch> : AzureSearchRepositoryAdapter<TDataSearch, TDataSearch>
#pragma warning restore SA1402 // File may only contain a single type
    where TDataSearch : IAzureSearch
{
    public AzureSearchRepositoryAdapter(IPersistentDataRepository<TDataSearch> repository)
        : base(repository)
    {
    }
}
