// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace AzureExtension.DataManager;

public interface IAzureLiveDataProvider
{
    Task<TeamProject> GetTeamProject(Uri connection, string id);

    Task<GitRepository> GetRepositoryAsync(string projectId, string repositoryId, CancellationToken cancellationToken);
}
