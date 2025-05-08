// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.PersistentData;

namespace AzureExtension.Controls;

#pragma warning disable SA1649 // File name should match first type name
public class AzureSearchRepositoryAdapter<TDataSearch> : IAzureSearchRepository
#pragma warning restore SA1649 // File name should match first type name
    where TDataSearch : IAzureSearch
{
    private readonly IPersistentDataRepository<TDataSearch> _repository;

    public AzureSearchRepositoryAdapter(IPersistentDataRepository<TDataSearch> repository)
    {
        _repository = repository;
    }

    public void Remove(IAzureSearch azureSearch)
    {
        _repository.RemoveSavedData((TDataSearch)azureSearch);
    }
}
