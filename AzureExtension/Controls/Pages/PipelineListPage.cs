// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.DataFactory;
using Azure.ResourceManager.DataFactory.Models;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Pages;

public class PipelineListPage : ListPage
{
    public PipelineListPage()
    {
    }

    public override IListItem[] GetItems()
    {
        var item = GetPipelineListItemsAsync().Result;
        return new IListItem[]
        {
            item,
        };
    }

    private async Task<DataFactoryPipelineRunInfo> GetPipelineAsync()
    {
        // authenticate your client
        TokenCredential cred = new DefaultAzureCredential();
        ArmClient client = new ArmClient(cred);

        // this example assumes you already have this DataFactoryResource created on azure
        // for more information of creating DataFactoryResource, please refer to the document of DataFactoryResource
        string subscriptionId = "12345678-1234-1234-1234-12345678abc";
        string resourceGroupName = "exampleResourceGroup";
        string factoryName = "exampleFactoryName";
        ResourceIdentifier dataFactoryResourceId = DataFactoryResource.CreateResourceIdentifier(subscriptionId, resourceGroupName, factoryName);
        DataFactoryResource dataFactory = client.GetDataFactoryResource(dataFactoryResourceId);

        // invoke the operation
        string runId = "2f7fdb90-5df1-4b8e-ac2f-064cfa58202b";
        DataFactoryPipelineRunInfo result = await dataFactory.GetPipelineRunAsync(runId);

        Console.WriteLine($"Succeeded: {result}");

        return result;
    }

    private async Task<ListItem> GetPipelineListItemsAsync()
    {
        var pipelineRunInfos = await GetPipelineAsync();
        var listItem = new ListItem(new NoOpCommand())
        {
            Title = pipelineRunInfos.PipelineName,
            Subtitle = pipelineRunInfos.Status.ToString(),
            Icon = new IconInfo("\uE8A7"),
        };

        return listItem;
    }
}
