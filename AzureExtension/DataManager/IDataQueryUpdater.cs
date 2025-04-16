// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls;

namespace AzureExtension.DataManager;

public interface IDataQueryUpdater
{
    bool IsNewOrStale(IQuery query, TimeSpan refreshCooldown);

    Task UpdateQueryAsync(IQuery query, CancellationToken cancellationToken);
}
