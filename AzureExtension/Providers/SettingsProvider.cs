// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

<<<<<<< HEAD
using AzureExtension.Controls.Pages;
=======
using AzureExtension.DataManager;
using AzureExtension.Pages;
>>>>>>> main
using CommandPaletteAzureExtension.Helpers;
using Serilog;

namespace AzureExtension.Providers;

public class SettingsProvider() : ISettingsProvider
{
    private static readonly Lazy<ILogger> _logger = new(() => Log.ForContext("SourceContext", nameof(SettingsProvider)));

    private static readonly ILogger _log = _logger.Value;

    string ISettingsProvider.DisplayName => Resources.GetResource(@"SettingsProviderDisplayName", _log);

<<<<<<< HEAD
    public AdaptiveCardSessionResult GetSettingsAdaptiveCardSession()
    {
        _log.Information($"GetSettingsAdaptiveCardSession");
        return new AdaptiveCardSessionResult(new SettingsUIController());
=======
    public AdaptiveCardSessionResult GetSettingsAdaptiveCardSession(CacheManager cacheManager)
    {
        _log.Information($"GetSettingsAdaptiveCardSession");
        return new AdaptiveCardSessionResult(new SettingsUIController(cacheManager));
>>>>>>> main
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
