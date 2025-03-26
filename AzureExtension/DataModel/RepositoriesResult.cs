// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace AzureExtension.DataModel
{
    public class RepositoriesResult
    {
        public RepositoriesResult(IEnumerable<IRepository> repositories)
        {
            Repositories = repositories;
            Result = new ProviderOperationResult(ProviderOperationStatus.Success, null, string.Empty, string.Empty);
        }

        public RepositoriesResult(Exception error, string diagnosticText)
        {
            Repositories = new List<IRepository>();
            Result = new ProviderOperationResult(ProviderOperationStatus.Failure, error, string.Empty, diagnosticText);
        }

        public IEnumerable<IRepository> Repositories { get; }

        public ProviderOperationResult Result { get; }
    }
}
