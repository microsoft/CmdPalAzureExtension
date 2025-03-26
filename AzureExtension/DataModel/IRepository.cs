// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace AzureExtension.DataModel;

public interface IRepository
{
    string DisplayName { get; }

    string OwningAccountName { get; }

    bool IsPrivate { get; }

    DateTime LastUpdated { get; }

    Uri RepoUri { get; }
}
