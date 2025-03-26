// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Client;
using AzureExtension.DataModel;
using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace AzureExtension.Providers;

public class CommandPaletteRepository : IRepository
{
    private readonly string _name;

    private readonly Uri _cloneUrl;

    private readonly bool _isPrivate;

    private readonly DateTimeOffset _lastUpdated;

    string IRepository.DisplayName => _name;

    public string OwningAccountName => _owningAccountName;

    private readonly string _owningAccountName = string.Empty;

    public bool IsPrivate => _isPrivate;

    public DateTimeOffset LastUpdated => _lastUpdated;

    public Uri RepoUri => _cloneUrl;

    DateTime IRepository.LastUpdated => throw new NotImplementedException();

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandPaletteRepository"/> class.
    /// </summary>
    /// <remarks>
    /// This can throw.
    /// </remarks>
    public CommandPaletteRepository(GitRepository gitRepository)
    {
        _name = gitRepository.Name;

        Uri? localUrl;
        if (!Uri.TryCreate(gitRepository.WebUrl, UriKind.RelativeOrAbsolute, out localUrl))
        {
            throw new ArgumentException("URl is null");
        }

        var repoInformation = new AzureUri(localUrl);
        _owningAccountName = Path.Join(repoInformation.Connection.Host, repoInformation.Organization, repoInformation.Project);

        _cloneUrl = localUrl;

        _isPrivate = gitRepository.ProjectReference.Visibility == Microsoft.TeamFoundation.Core.WebApi.ProjectVisibility.Private;
        _lastUpdated = DateTimeOffset.UtcNow;
    }

    public CommandPaletteRepository(DataModel.Repository repository)
    {
        _name = repository.Name;
        _owningAccountName = Path.Join(repository.Clone.Connection.Host, repository.Clone.Organization, repository.Clone.Project);
        _cloneUrl = repository.Clone.Uri;
        _isPrivate = repository.Private;
        _lastUpdated = new(repository.UpdatedAt);
    }
}
