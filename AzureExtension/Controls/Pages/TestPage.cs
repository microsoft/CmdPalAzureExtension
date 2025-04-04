// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Pages;

public partial class TestPage : ListPage
{
    private readonly IAzureDataManager _azureDataManager;

    public TestPage(IAzureDataManager azureDataManager)
    {
        _azureDataManager = azureDataManager;
    }

    public override IListItem[] GetItems()
    {
        var res = _azureDataManager.GetPipelineDataAsync(new Uri("https://dev.azure.com/microsoft/Dart/")).GetAwaiter().GetResult();

        foreach (var item in res)
        {
            Console.WriteLine(item);
        }

        return [];
    }
}
