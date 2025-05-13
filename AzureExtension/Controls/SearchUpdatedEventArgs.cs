// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace AzureExtension.Controls;

public enum SearchUpdatedEventType
{
    SearchAdded,
    SearchRemoved,
    SearchRemoving,
}

public enum SearchUpdatedType
{
    Unknown = 0,
    Query,
    PullRequest,
    Pipeline,
}

public class SearchUpdatedEventArgs : EventArgs
{
    public IAzureSearch? AzureSearch { get; }

    public Exception? Exception { get; set; } = null!;

    public bool Success => Exception == null;

    public SearchUpdatedEventType EventType { get; }

    public SearchUpdatedType SearchType { get; }

    public SearchUpdatedEventArgs(IAzureSearch? azureSearch, SearchUpdatedEventType eventType, SearchUpdatedType searchType, Exception? ex = null)
    {
        AzureSearch = azureSearch;
        EventType = eventType;
        SearchType = searchType;
        Exception = ex;
    }
}
