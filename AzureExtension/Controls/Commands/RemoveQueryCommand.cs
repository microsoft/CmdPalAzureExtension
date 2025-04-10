// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Commands;

public partial class RemoveQueryCommand : InvokableCommand
{
    private readonly Query savedQuery;
    private readonly IResources _resources;
    private readonly SavedQueriesMediator _savedQueriesMediator;

    public RemoveQueryCommand(Query query, IResources resources, SavedQueriesMediator savedQueriesMediator)
    {
        _resources = resources;
        _savedQueriesMediator = savedQueriesMediator;

        savedQuery = query;
        Name = _resources.GetResource("Commands_Remove_Saved_Query");
        Icon = new IconInfo("\uecc9");
    }

    public override CommandResult Invoke()
    {
       _savedQueriesMediator.RemoveQuery(savedQuery);

       return CommandResult.KeepOpen();
    }
}
