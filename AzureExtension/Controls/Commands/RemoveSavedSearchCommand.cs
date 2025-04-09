// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Commands;

public partial class RemoveSavedSearchCommand : InvokableCommand
{
    private readonly Query savedSearch;
    private readonly IResources _resources;
    private readonly SavedSearchesMediator _savedSearchesMediator;

    public RemoveSavedSearchCommand(Query search, IResources resources, SavedSearchesMediator savedSearchesMediator)
    {
        _resources = resources;
        _savedSearchesMediator = savedSearchesMediator;

        savedSearch = search;
        Name = _resources.GetResource("Commands_Remove_Saved_Search");
        Icon = new IconInfo("\uecc9");
    }

    public override CommandResult Invoke()
    {
       _savedSearchesMediator.RemoveSearch(savedSearch);

       return CommandResult.KeepOpen();
    }
}
