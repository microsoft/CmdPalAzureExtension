// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Commands;

public partial class RemoveCommand : InvokableCommand
{
    private readonly IAzureSearch _savedAzureSearch;
    private readonly IResources _resources;
    private readonly SavedAzureSearchesMediator _savedAzureSearchesMediator;
    private readonly IAzureSearchRepository _azureSearchRepository;

    public RemoveCommand(IAzureSearch azureSearch, IResources resources, SavedAzureSearchesMediator savedAzureSearchesMediator, IAzureSearchRepository azureSearchRepository)
    {
        _resources = resources;
        _savedAzureSearchesMediator = savedAzureSearchesMediator;
        _azureSearchRepository = azureSearchRepository;
        _savedAzureSearch = azureSearch;
        Name = GetCommandNameFromSearchType();
        Icon = IconLoader.GetIcon("Remove");
    }

    public override CommandResult Invoke()
    {
        _azureSearchRepository.Remove(_savedAzureSearch);
        _savedAzureSearchesMediator.Remove(_savedAzureSearch);

        return CommandResult.KeepOpen();
    }

    private string GetCommandNameFromSearchType()
    {
        return _savedAzureSearch switch
        {
            IQuerySearch => _resources.GetResource("Commands_Remove_Query"),
            IPullRequestSearch => _resources.GetResource("Commands_Remove_PullRequestSearch"),
            IPipelineDefinitionSearch => _resources.GetResource("Commands_Remove_PipelineSearch"),
            _ => _resources.GetResource("Commands_Remove"),
        };
    }
}
