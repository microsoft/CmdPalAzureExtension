// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Windows.ApplicationModel;

namespace AzureExtension.Contracts;

public interface IPackagesService
{
    public bool IsPackageInstalled(string packageName);

    public PackageVersion GetPackageInstalledVersion(string packageName);
}
