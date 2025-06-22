// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls;

namespace AzureExtension.Helpers;

public static class SearchHelper
{
    public static SearchUpdatedType GetSearchUpdatedType(IAzureSearch? search)
    {
        if (search is IQuerySearch)
        {
            return SearchUpdatedType.Query;
        }
        else if (search is IPullRequestSearch)
        {
            return SearchUpdatedType.PullRequest;
        }
        else if (search is IPipelineDefinitionSearch)
        {
            return SearchUpdatedType.Pipeline;
        }

        return SearchUpdatedType.Unknown;
    }

    public static SearchUpdatedType GetSearchUpdatedType<TSearch>()
    where TSearch : IAzureSearch
    {
        if (typeof(TSearch) == typeof(IQuerySearch))
        {
            return SearchUpdatedType.Query;
        }
        else if (typeof(TSearch) == typeof(IPullRequestSearch))
        {
            return SearchUpdatedType.PullRequest;
        }
        else if (typeof(TSearch) == typeof(IPipelineDefinitionSearch))
        {
            return SearchUpdatedType.Pipeline;
        }

        return SearchUpdatedType.Unknown;
    }
}
