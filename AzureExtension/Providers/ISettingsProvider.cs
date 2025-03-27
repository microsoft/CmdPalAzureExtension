// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls.Pages;
using AzureExtension.DataManager;

namespace AzureExtension.Providers;

public interface ISettingsProvider : IDisposable
{
    string DisplayName { get; }

    AdaptiveCardSessionResult GetSettingsAdaptiveCardSession(CacheManager cacheManager);
}
