// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.DataModel;

namespace AzureExtension.DeveloperId
{
    public class DeveloperIdResult
    {
        public DeveloperIdResult(IDeveloperId developerId)
        {
            DeveloperId = developerId;
            Result = new ProviderOperationResult(ProviderOperationStatus.Success, null, string.Empty, string.Empty);
        }

        public DeveloperIdResult(Exception error, string diagnosticText)
        {
            DeveloperId = null;
            Result = new ProviderOperationResult(ProviderOperationStatus.Failure, error, string.Empty, diagnosticText);
        }

        public IDeveloperId? DeveloperId { get; }

        public ProviderOperationResult Result { get; }
    }
}
