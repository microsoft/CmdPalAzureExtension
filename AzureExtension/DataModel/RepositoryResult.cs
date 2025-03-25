// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace AzureExtension.DataModel
{
    public class RepositoryResult
    {
        public RepositoryResult(IRepository repository)
        {
            Repository = repository;
            Result = new ProviderOperationResult(ProviderOperationStatus.Success, null, string.Empty, string.Empty);
        }

        public RepositoryResult(Exception error, string diagnosticText)
        {
            Repository = null;
            Result = new ProviderOperationResult(ProviderOperationStatus.Failure, error, string.Empty, diagnosticText);
        }

        public IRepository? Repository { get; }

        public ProviderOperationResult Result { get; }
    }
}
