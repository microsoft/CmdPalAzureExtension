// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Helpers;
using AzureExtension.PersistentData;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Commands;

public partial class RemovePullRequestSearchCommand : InvokableCommand
{
    private readonly IPullRequestSearch _savedPullRequestSearch;
    private readonly IResources _resources;
    private readonly SavedQueriesMediator _savedQueriesMediator;
    private readonly ISavedPullRequestSearchRepository _pullRequestSearchRepository;

    public RemovePullRequestSearchCommand(IPullRequestSearch pullRequestSearch, IResources resources, SavedQueriesMediator savedQueriesMediator, ISavedPullRequestSearchRepository pullRequestSearchRepository)
    {
        _resources = resources;
        _savedQueriesMediator = savedQueriesMediator;
        _pullRequestSearchRepository = pullRequestSearchRepository;
        _savedPullRequestSearch = pullRequestSearch;
        Name = _resources.GetResource("Commands_Remove_Saved_Search");
        Icon = new IconInfo("\uecc9");
    }

    public override CommandResult Invoke()
    {
        _pullRequestSearchRepository.RemoveSavedPullRequestSearch(_savedPullRequestSearch).Wait();
        _savedQueriesMediator.RemovePullRequestSearch(_savedPullRequestSearch);

        return CommandResult.KeepOpen();
    }
}
