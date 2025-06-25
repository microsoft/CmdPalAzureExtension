// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace AzureExtension.Controls;

public class SearchSetLoadingStateArgs
{
    public bool IsLoading { get; set; }

    public SearchUpdatedType SearchType { get; set; } = SearchUpdatedType.Unknown;

    public SearchSetLoadingStateArgs(bool isLoading, SearchUpdatedType searchType)
    {
        IsLoading = isLoading;
        SearchType = searchType;
    }
}
