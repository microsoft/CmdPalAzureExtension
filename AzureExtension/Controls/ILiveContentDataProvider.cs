﻿// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.DataManager.Cache;
using AzureExtension.Helpers;

namespace AzureExtension.Controls;

#pragma warning disable SA1649 // File name should match first type name
public interface ILiveContentDataProvider<TContentDataType>
{
    Task<IEnumerable<TContentDataType>> GetContentData(IAzureSearch search);

    event EventHandler<CacheManagerUpdateEventArgs> WeakOnUpdate;

    event EventHandler<CacheManagerUpdateEventArgs> OnUpdate;
}
