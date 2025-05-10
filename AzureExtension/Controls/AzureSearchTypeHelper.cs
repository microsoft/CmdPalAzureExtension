// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace AzureExtension.Controls;

public static class AzureSearchTypeHelper
{
    public static Type GetSearchType(this IAzureSearch search)
    {
        return search switch
        {
            IQuery => typeof(IQuery),
            IPullRequestSearch => typeof(IPullRequestSearch),
            IPipelineDefinitionSearch => typeof(IPipelineDefinitionSearch),
            _ => throw new ArgumentException($"Unknown search type: {search.GetType().Name}"),
        };
    }

    public static Type GetDataObjectType(this IAzureSearch search)
    {
        return search switch
        {
            IQuery => typeof(IWorkItem),
            IPullRequestSearch => typeof(IPullRequest),
            IPipelineDefinitionSearch => typeof(IBuild),
            _ => throw new ArgumentException($"Unknown search type: {search.GetType().Name}"),
        };
    }

    public static Type GetSearchDataType(this IAzureSearch search)
    {
        return search switch
        {
            IQuery => typeof(IQuery),
            IPullRequestSearch => typeof(IPullRequestSearch),
            IPipelineDefinitionSearch => typeof(IDefinition),
            _ => throw new ArgumentException($"Unknown search type: {search.GetType().Name}"),
        };
    }
}
