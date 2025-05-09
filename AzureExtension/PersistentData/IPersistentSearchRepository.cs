// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls;

namespace AzureExtension.PersistentData;

#pragma warning disable SA1649 // File name should match first type name
public interface IPersistentSearchRepository<TDataSearch> : ISavedSearchesUpdater<TDataSearch>, ISavedSearchesProvider<TDataSearch>
    where TDataSearch : IAzureSearch
{
}
