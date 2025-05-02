// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Identity.Client;
using Microsoft.VisualStudio.Services.WebApi;

namespace AzureExtension.Client;

public interface IConnectionProvider
{
    Guid GetAuthorizedEntityId(Uri connection, IAccount account);

    Task<IVssConnection> GetVssConnectionAsync(Uri uri, IAccount account);
}
