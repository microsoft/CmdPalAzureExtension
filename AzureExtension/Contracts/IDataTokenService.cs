// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.DeveloperId;

namespace AzureExtension.Contracts;

public interface IDataTokenService
{
    public Task<string> GetTokenAsync(IDeveloperId? devId);
}
