// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.DataModel;

namespace AzureExtension.Pages
{
    public class AdaptiveCardSessionResult
    {
        public AdaptiveCardSessionResult(IExtensionAdaptiveCardSession adaptiveCardSession)
        {
            AdaptiveCardSession = adaptiveCardSession;
            Result = new ProviderOperationResult(ProviderOperationStatus.Success, null, string.Empty, string.Empty);
        }

        public AdaptiveCardSessionResult(Exception error, string diagnosticText)
        {
            AdaptiveCardSession = null;
            Result = new ProviderOperationResult(ProviderOperationStatus.Failure, error, string.Empty, diagnosticText);
        }

        public IExtensionAdaptiveCardSession? AdaptiveCardSession { get; }

        public ProviderOperationResult Result { get; }
    }
}
