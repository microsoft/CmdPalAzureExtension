// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace AzureExtension.DataModel
{
    public class RepositoriesSearchResult
    {
        public RepositoriesSearchResult(IEnumerable<IRepository> repositories)
        {
            Repositories = repositories;
            Result = new ProviderOperationResult(ProviderOperationStatus.Success, null, string.Empty, string.Empty);
        }

        public RepositoriesSearchResult(IEnumerable<IRepository> repositories, string selectionOptionsLabel, string[] selectionOptions, string selectionOptionsName)
        {
            Repositories = repositories;
            SelectionOptionsLabel = selectionOptionsLabel;
            SelectionOptions = selectionOptions;
            SelectionOptionsName = selectionOptionsName;
            Result = new ProviderOperationResult(ProviderOperationStatus.Success, null, string.Empty, string.Empty);
        }

        public RepositoriesSearchResult(Exception error, string diagnosticText)
        {
            Repositories = new List<IRepository>();
            Result = new ProviderOperationResult(ProviderOperationStatus.Failure, error, string.Empty, diagnosticText);
        }

        public IEnumerable<IRepository>? Repositories { get; }

        public string? SelectionOptionsLabel { get; }

        public string[]? SelectionOptions { get; }

        public string? SelectionOptionsName { get; }

        public ProviderOperationResult Result { get; }
    }
}
