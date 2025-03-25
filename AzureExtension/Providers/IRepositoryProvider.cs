// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.DataModel;
using AzureExtension.DeveloperId;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace AzureExtension.Providers
{
    public interface IRepositoryProvider
    {
        string DisplayName { get; }

        IRandomAccessStreamReference Icon { get; }

        IAsyncOperation<RepositoriesResult> GetRepositoriesAsync(IDeveloperId developerId);

        IAsyncOperation<RepositoryUriSupportResult> IsUriSupportedAsync(Uri uri);

        IAsyncOperation<RepositoryUriSupportResult> IsUriSupportedAsync(Uri uri, IDeveloperId developerId);

        IAsyncOperation<RepositoryResult> GetRepositoryFromUriAsync(Uri uri);

        IAsyncOperation<RepositoryResult> GetRepositoryFromUriAsync(Uri uri, IDeveloperId developerId);

        IAsyncOperation<ProviderOperationResult> CloneRepositoryAsync(IRepository repository, string cloneDestination);

        IAsyncOperation<ProviderOperationResult> CloneRepositoryAsync(IRepository repository, string cloneDestination, IDeveloperId developerId);
    }
}
