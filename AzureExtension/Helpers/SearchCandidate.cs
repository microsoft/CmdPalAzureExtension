// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls;
using AzureExtension.DataModel;
using AzureExtension.DeveloperId;

namespace AzureExtension.Helpers;

public class SearchCandidate : ISearch
{
    public string Name { get; set; } = string.Empty;

    public string SearchString { get; set; } = string.Empty;

    public bool IsTopLevel { get; set; }

    public IDeveloperId DeveloperId => throw new NotImplementedException();

    public Query Query => throw new NotImplementedException();

    public SearchCandidate()
    {
    }

    public SearchCandidate(string name, string searchString)
    {
        Name = name;
        SearchString = searchString;
    }

    public SearchCandidate(string name, string searchString, bool isTopLevel)
    {
        Name = name;
        SearchString = searchString;
        IsTopLevel = isTopLevel;
    }
}
