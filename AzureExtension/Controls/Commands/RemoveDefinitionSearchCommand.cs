// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Helpers;
using AzureExtension.PersistentData;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Commands;

public partial class RemoveDefinitionSearchCommand : InvokableCommand
{
    private readonly IDefinitionSearch _savedDefinitionSearch;
    private readonly IResources _resources;
    private readonly SavedAzureSearchesMediator _savedDefinitionSearchesMediator;
    private readonly IDefinitionRepository _definitionRepository;

    public RemoveDefinitionSearchCommand(IDefinitionSearch azureSearch, IResources resources, SavedAzureSearchesMediator savedDefinitionSearchesMediator, IDefinitionRepository definitionRepository)
    {
        _resources = resources;
        _savedDefinitionSearchesMediator = savedDefinitionSearchesMediator;
        _definitionRepository = definitionRepository;
        _savedDefinitionSearch = azureSearch;
        Name = _resources.GetResource("Commands_Remove_Search");
        Icon = IconLoader.GetIcon("Remove");
    }

    public override CommandResult Invoke()
    {
        _definitionRepository.RemoveSavedDefinitionSearch(_savedDefinitionSearch);
        _savedDefinitionSearchesMediator.Remove(_savedDefinitionSearch);

        return CommandResult.KeepOpen();
    }
}
