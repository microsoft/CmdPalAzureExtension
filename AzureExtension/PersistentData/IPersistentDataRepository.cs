// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls;
using Microsoft.Identity.Client;

namespace AzureExtension.PersistentData;

#pragma warning disable SA1649 // File name should match first type name
public interface IPersistentDataRepository<TDataSearch, TDataResult>
    where TDataSearch : IAzureSearch
{
    void RemoveSavedData(TDataSearch dataSearch);

    TDataResult GetSavedData(TDataSearch dataSearch);

    IEnumerable<TDataResult> GetAllSavedData(bool getTopLevelOnly = false);

    bool IsTopLevel(TDataSearch dataSearch);

    Task AddOrUpdateData(TDataSearch dataSearch, bool isTopLevel, IAccount account);
}

public interface IPersistentDataRepository<TData> : IPersistentDataRepository<TData, TData>
    where TData : IAzureSearch
{
}
