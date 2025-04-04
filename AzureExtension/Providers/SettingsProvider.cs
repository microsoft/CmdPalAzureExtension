// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls.Pages;
using AzureExtension.DataManager;
using AzureExtension.Helpers;
using Serilog;

namespace AzureExtension.Providers;

public class SettingsProvider : ISettingsProvider
{
    private static readonly Lazy<ILogger> _logger = new(() => Log.ForContext("SourceContext", nameof(SettingsProvider)));

    private static readonly ILogger _log = _logger.Value;

    string ISettingsProvider.DisplayName => _resources.GetResource(@"SettingsProviderDisplayName", _log);

    private readonly IResources _resources;

    private readonly CacheManager _cacheManager;

    public SettingsProvider(IResources resources, CacheManager cacheManager)
    {
        _log.Debug($"SettingsProvider constructor");
        _resources = resources;
        _cacheManager = cacheManager;
    }

    public AdaptiveCardSessionResult GetSettingsAdaptiveCardSession(CacheManager cacheManager, IResources resources)
    {
        _log.Information($"GetSettingsAdaptiveCardSession");
        return new AdaptiveCardSessionResult(new SettingsUIController(cacheManager, resources));
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
