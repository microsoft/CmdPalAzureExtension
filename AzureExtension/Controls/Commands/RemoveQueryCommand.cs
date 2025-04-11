// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Helpers;
using AzureExtension.PersistentData;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Commands;

public partial class RemoveQueryCommand : InvokableCommand
{
    private readonly IQuery savedQuery;
    private readonly IResources _resources;
    private readonly SavedQueriesMediator _savedQueriesMediator;
    private readonly IQueryRepository _queryRepository;

    public RemoveQueryCommand(IQuery query, IResources resources, SavedQueriesMediator savedQueriesMediator, IQueryRepository queryRepository)
    {
        _resources = resources;
        _savedQueriesMediator = savedQueriesMediator;
        _queryRepository = queryRepository;
        savedQuery = query;
        Name = _resources.GetResource("Commands_Remove_Saved_Search");
        Icon = new IconInfo("\uecc9");
    }

    public override CommandResult Invoke()
    {
        _queryRepository.RemoveSavedQueryAsync(savedQuery).Wait();
        _savedQueriesMediator.RemoveQuery(savedQuery);

        return CommandResult.KeepOpen();
    }
}
