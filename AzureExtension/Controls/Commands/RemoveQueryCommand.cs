// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Helpers;
using AzureExtension.PersistentData;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Commands;

public partial class RemoveQueryCommand : InvokableCommand
{
    private readonly IAzureSearch _savedAzureSearch;
    private readonly IResources _resources;
    private readonly SavedQueriesMediator _savedQueriesMediator;
    private readonly IAzureSearchRepository _azureSearchRepository;

    public RemoveQueryCommand(IAzureSearch azureSearch, IResources resources, SavedQueriesMediator savedQueriesMediator, IAzureSearchRepository azureSearchRepository)
    {
        _resources = resources;
        _savedQueriesMediator = savedQueriesMediator;
        _azureSearchRepository = azureSearchRepository;
        _savedAzureSearch = azureSearch;
        Name = _resources.GetResource("Commands_Remove_Saved_Search");
        Icon = new IconInfo("\uecc9");
    }

    public override CommandResult Invoke()
    {
        _azureSearchRepository.Remove(_savedAzureSearch).Wait();
        _savedQueriesMediator.RemoveQuery(_savedAzureSearch);

        return CommandResult.KeepOpen();
    }
}
