// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.DeveloperId;

namespace AzureExtension.Contracts;

// ARMTokenService is a service that provides an Azure Resource Manager (ARM) token.
public interface IArmTokenService
{
    public Task<string> GetTokenAsync(IDeveloperId? devId);
}
