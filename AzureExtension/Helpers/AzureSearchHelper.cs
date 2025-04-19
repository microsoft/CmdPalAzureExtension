// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls;

namespace AzureExtension.Helpers;

public class AzureSearchHelper
{
    public static bool IsIQuery(IAzureSearch search)
    {
        var interfaces = search.GetType().GetInterfaces();
        if (interfaces.Length == 0)
        {
            return false;
        }

        foreach (var i in interfaces)
        {
            if (i == typeof(IQuery))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        return false;
    }

    public static bool IsIPullRequestSearch(IAzureSearch search)
    {
        var interfaces = search.GetType().GetInterfaces();
        if (interfaces.Length == 0)
        {
            return false;
        }

        foreach (var i in interfaces)
        {
            if (i == typeof(IPullRequestSearch))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        return false;
    }
}
