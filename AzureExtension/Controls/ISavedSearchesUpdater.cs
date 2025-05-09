// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Identity.Client;

namespace AzureExtension.Controls;

#pragma warning disable SA1649 // File name should match first type name
public interface ISavedSearchesUpdater<TDataSearch>
#pragma warning restore SA1649 // File name should match first type name
    where TDataSearch : IAzureSearch
{
    void RemoveSavedSearch(TDataSearch search);

    void AddOrUpdateSearch(TDataSearch search, bool isTopLevel);

    bool IsTopLevel(TDataSearch search);

    Task Validate(TDataSearch search, IAccount account);
}
