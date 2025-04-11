// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Helpers;
using AzureExtension.PersistentData;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Commands;

public partial class RemoveQueryCommand : InvokableCommand
{
    private readonly IQuery savedSearch;
    private readonly IResources _resources;
    private readonly SavedQueriesMediator _savedSearchesMediator;
    private readonly IQueryRepository _queryRepository;

    public RemoveQueryCommand(IQuery search, IResources resources, SavedQueriesMediator savedSearchesMediator, IQueryRepository queryRepository)
    {
        _resources = resources;
        _savedSearchesMediator = savedSearchesMediator;
        _queryRepository = queryRepository;
        savedSearch = search;
        Name = _resources.GetResource("Commands_Remove_Saved_Search");
        Icon = new IconInfo("\uecc9");
    }

    public override CommandResult Invoke()
    {
        _queryRepository.RemoveSavedQueryAsync(savedSearch).Wait();
        _savedSearchesMediator.RemoveSearch(savedSearch);

        return CommandResult.KeepOpen();
    }
}
