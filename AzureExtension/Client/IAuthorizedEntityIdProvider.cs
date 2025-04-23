// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Identity.Client;

namespace AzureExtension.Client;

public interface IAuthorizedEntityIdProvider
{
    Guid GetAuthorizedEntityId(Uri connection, IAccount account);
}
