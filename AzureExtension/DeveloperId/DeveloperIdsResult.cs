// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.DataModel;

namespace AzureExtension.DeveloperId
{
    public class DeveloperIdsResult
    {
        public DeveloperIdsResult(IEnumerable<IDeveloperId> developerIds)
        {
            DeveloperIds = developerIds;
            Result = new ProviderOperationResult(ProviderOperationStatus.Success, null, string.Empty, string.Empty);
        }

        public DeveloperIdsResult(Exception? error, string diagnosticText)
        {
            DeveloperIds = new List<IDeveloperId>();
            Result = new ProviderOperationResult(ProviderOperationStatus.Failure, error, string.Empty, diagnosticText);
        }

        public IEnumerable<IDeveloperId> DeveloperIds { get; }

        public ProviderOperationResult Result { get; }
    }
}
