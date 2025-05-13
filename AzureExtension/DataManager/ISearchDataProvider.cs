// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls;

namespace AzureExtension.DataManager;

#pragma warning disable SA1649 // File name should match first type name
public interface ISearchDataProvider<TDataSearch, TDataObject>
#pragma warning restore SA1649 // File name should match first type name
    where TDataSearch : IAzureSearch
{
    TDataObject? GetDataForSearch(TDataSearch search);
}

public interface ISearchDataProvider
{
    object? GetDataForSearch(IAzureSearch search);
}
