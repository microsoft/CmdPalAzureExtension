// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Client;
using AzureExtension.Controls;
using Microsoft.Identity.Client;

namespace AzureExtension.PersistentData;

public partial class PersistentDataManager
{
    public Task AddSavedDefinitionSearch(IDefinitionSearch definitionSearch)
    {
        ValidateDataStore();
        var internalId = definitionSearch.InternalId;
        var projectUrl = definitionSearch.ProjectUrl;

        _log.Information($"Adding definition search: {internalId} - {projectUrl}.");
        if (DefinitionSearch.Get(_dataStore, internalId, projectUrl) != null)
        {
            _log.Error($"Definition search {internalId} - {projectUrl} already exists.");
            return Task.CompletedTask;
        }

        DefinitionSearch.Add(_dataStore, internalId, projectUrl);
        return Task.CompletedTask;
    }

    public IDefinitionSearch GetDefinitionSearch(long internalId, string projectUrl)
    {
        ValidateDataStore();
        return DefinitionSearch.Get(_dataStore, internalId, projectUrl) ?? throw new InvalidOperationException($"Definition search {internalId} - {projectUrl} not found.");
    }

    public Task<IEnumerable<IDefinitionSearch>> GetAllDefinitionSearchesAsync(bool includeTopLevel)
    {
        ValidateDataStore();
        if (includeTopLevel)
        {
            return Task.FromResult(DefinitionSearch.GetTopLevel(_dataStore));
        }

        return Task.FromResult(DefinitionSearch.GetAll(_dataStore));
    }

    public Task<IEnumerable<IDefinitionSearch>> GetSavedDefinitionSearches()
    {
        return GetAllDefinitionSearchesAsync(false);
    }

    public Task<IEnumerable<IDefinitionSearch>> GetTopLevelDefinitionSearches()
    {
        return GetAllDefinitionSearchesAsync(true);
    }

    public Task<bool> IsTopLevel(IDefinitionSearch definitionSearch)
    {
        ValidateDataStore();
        var dsDefinitionSearch = DefinitionSearch.Get(_dataStore, definitionSearch.InternalId, definitionSearch.ProjectUrl);
        return Task.FromResult(dsDefinitionSearch != null && dsDefinitionSearch.IsTopLevel);
    }

    public Task RemoveSavedDefinitionSearch(IDefinitionSearch definitionSearch)
    {
        ValidateDataStore();
        var internalId = definitionSearch.InternalId;
        var projectUrl = definitionSearch.ProjectUrl;

        _log.Information($"Removing definition search: {internalId} - {projectUrl}.");
        if (DefinitionSearch.Get(_dataStore, internalId, projectUrl) == null)
        {
            _log.Error($"Definition search {internalId} - {projectUrl} not found.");
            return Task.CompletedTask;
        }

        DefinitionSearch.Remove(_dataStore, internalId, projectUrl);
        return Task.CompletedTask;
    }

    public bool ValidateDefinitionSearch(IDefinitionSearch definitionSearch, IAccount account)
    {
        var definitionInfo = _azureValidator.GetDefinitionInfo(definitionSearch.ProjectUrl, definitionSearch.InternalId, account);
        return definitionInfo.Result == ResultType.Success;
    }

    public void UpdateDefinitionSearchTopLevelStatus(IDefinitionSearch definitionSearch, bool isTopLevel, IAccount account)
    {
        ValidateDataStore();
        ValidateDefinitionSearch(definitionSearch, account);
        DefinitionSearch.AddOrUpdate(_dataStore, definitionSearch.InternalId, definitionSearch.ProjectUrl, isTopLevel);
    }
}
