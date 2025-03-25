// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace AzureExtension.Providers;

internal sealed class SettingsCardData
{
    public string NotificationsEnabled { get; set; } = string.Empty;

    public string CacheLastUpdated { get; set; } = string.Empty;

    public string UpdateAzureData { get; set; } = string.Empty;
}
