// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.SearchPages;

public abstract partial class SearchPage<T> : ListPage
{
    public T CurrentSearch { get; private set; }

    public SearchPage(T search)
    {
        CurrentSearch = search;
        Icon = new IconInfo(AzureIcon.IconDictionary["logo"]);
        Name = "Name";
    }
}
