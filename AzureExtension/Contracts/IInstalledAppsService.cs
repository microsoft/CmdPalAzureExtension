// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace AzureExtension.Contracts;

public interface IInstalledAppsService
{
    public Task<HashSet<string>> CheckForMissingDependencies(
        HashSet<string> msixPackageFamilies,
        HashSet<string> win32AppDisplayNames,
        HashSet<string> vsCodeExtensions,
        Dictionary<string, List<string>> displayNameAndPathEntries);
}
