// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.PersistentData;
using Microsoft.CommandPalette.Extensions;

namespace AzureExtension.Controls.Pages;

public interface ISearchPageFactory
{
    IListItem CreateItemForSearch(IAzureSearch search, IAzureSearchRepository azureSearchRepository);
}
