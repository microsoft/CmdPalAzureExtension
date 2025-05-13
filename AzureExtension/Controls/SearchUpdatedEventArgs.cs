// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace AzureExtension.Controls;

public class SearchUpdatedEventArgs : EventArgs
{
    public IAzureSearch? AzureSearch { get; }

    public Exception? Exception { get; set; } = null!;

    public bool Success => Exception == null;

    public SearchUpdatedEventArgs(IAzureSearch? azureSearch, Exception? ex = null)
    {
        AzureSearch = azureSearch;
        Exception = ex;
    }
}
