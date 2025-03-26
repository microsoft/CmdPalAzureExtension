// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.DataModel;
using AzureExtension.DeveloperId;
using Windows.Foundation;

namespace AzureExtension.Providers
{
    public interface IRepositoryProvider2 : IRepositoryProvider
    {
        string[] SearchFieldNames { get; }

        bool IsSearchingSupported { get; }

        string AskToSearchLabel { get; }

        IAsyncOperation<IReadOnlyList<string>> GetValuesForSearchFieldAsync(IReadOnlyDictionary<string, string> fieldValues, string requestedSearchField, IDeveloperId developerId);

        IAsyncOperation<RepositoriesSearchResult> GetRepositoriesAsync(IReadOnlyDictionary<string, string> fieldValues, IDeveloperId developerId);
    }
}
