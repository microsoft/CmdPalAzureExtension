// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.DataModel;

namespace AzureExtension.Pages
{
    public interface IExtensionAdaptiveCardSession : IDisposable
    {
        ProviderOperationResult Initialize(IExtensionAdaptiveCard extensionUI);

        Task<ProviderOperationResult> OnActionAsync(string action, string inputs);
    }
}
