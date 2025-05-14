// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace AzureExtension.Controls;

#pragma warning disable SA1649 // File name should match first type name
public interface ILiveSearchDataProvider<TSearchDataType>
{
    Task<TSearchDataType> GetSearchData(IAzureSearch search);
}
