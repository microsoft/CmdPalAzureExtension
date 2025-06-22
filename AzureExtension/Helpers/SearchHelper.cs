// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Account;
using AzureExtension.Client;
using AzureExtension.Controls;
using AzureExtension.Controls.Forms;
using Microsoft.Identity.Client;

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

    public static InfoType GetInfoTypeFromSearch(IAzureSearch? search)
    {
        if (search is IQuerySearch)
        {
            return InfoType.Query;
        }
        else if (search is IPullRequestSearch)
        {
            return InfoType.Repository;
        }
        else if (search is IPipelineDefinitionSearch)
        {
            return InfoType.Definition;
        }

        return InfoType.Unknown;
    }

    public static Type GetAzureSearchType(IAzureSearch search)
    {
        if (search is IQuerySearch)
        {
            return typeof(IQuerySearch);
        }
        else if (search is IPullRequestSearch)
        {
            return typeof(IPullRequestSearch);
        }
        else if (search is IPipelineDefinitionSearch)
        {
            return typeof(IPipelineDefinitionSearch);
        }

        throw new NotImplementedException($"No type for search {search.GetType()}");
    }

    public static InfoResult GetSearchInfoFromSearch(IAzureSearch? search, AzureClientHelpers azureClientHelpers, IAccount account)
    {
        if (search == null)
        {
            throw new ArgumentNullException(nameof(search), "Search cannot be null.");
        }

        var infoType = GetInfoTypeFromSearch(search);

        if (infoType == InfoType.Definition && search is IPipelineDefinitionSearch pipelineSearch)
        {
            return azureClientHelpers.GetInfo(new AzureUri(search.Url), account, infoType, pipelineSearch.InternalId).Result;
        }

        return azureClientHelpers.GetInfo(new AzureUri(search.Url), account, infoType).Result;
    }

    public static string GetPipelineSearchName(IPipelineDefinitionSearch search, AzureClientHelpers azureClientHelpers, IAccountProvider accountProvider)
    {
        var info = GetSearchInfoFromSearch(search, azureClientHelpers, accountProvider.GetDefaultAccount());
        return info?.Name ?? string.Empty;
    }
}
