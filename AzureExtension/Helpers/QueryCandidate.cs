// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Client;
using AzureExtension.Controls;

namespace AzureExtension.Helpers;

public class QueryCandidate : IQuery
{
    public string DisplayName { get; set; } = string.Empty;

    public string QueryId { get; set; } = string.Empty;

    public string SearchString { get; set; } = string.Empty;

    public long ProjectId { get; set; }

    public string DeveloperLogin { get; set; } = string.Empty;

    public string QueryResults { get; set; } = string.Empty;

    public long QueryResultCount { get; set; }

    public bool IsTopLevel { get; set; }

    public AzureUri? Uri { get; set; }

    // Implement ISearch interface
    public string Name => DisplayName;

    public string Url => Uri?.OriginalString ?? string.Empty;

    public QueryCandidate(string displayName, string queryId, string searchString)
    {
        DisplayName = displayName;
        QueryId = queryId;
        SearchString = searchString;
    }

    public QueryCandidate(string displayName, string queryId, string searchString, long projectId, string developerLogin)
    {
        DisplayName = displayName;
        QueryId = queryId;
        SearchString = searchString;
        ProjectId = projectId;
        DeveloperLogin = developerLogin;
    }

    public QueryCandidate(string displayName, string queryId, string searchString, long projectId, string developerLogin, string queryResults, long queryResultCount, bool isTopLevel, AzureUri? azureUri)
    {
        DisplayName = displayName;
        QueryId = queryId;
        SearchString = searchString;
        ProjectId = projectId;
        DeveloperLogin = developerLogin;
        QueryResults = queryResults;
        QueryResultCount = queryResultCount;
        IsTopLevel = isTopLevel;
        Uri = azureUri;
    }
}
