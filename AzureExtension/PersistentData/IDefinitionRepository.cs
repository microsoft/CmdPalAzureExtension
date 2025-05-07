// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls;
using Microsoft.Identity.Client;

namespace AzureExtension.PersistentData;

public interface IDefinitionRepository
{
    Task<IDefinition> GetDefinition(IDefinitionSearch definitionSearch, IAccount account);

    Task<IEnumerable<IDefinition>> GetAllDefinitionsAsync(bool includeTopLevel, IAccount account);

    Task<IEnumerable<IDefinitionSearch>> GetSavedDefinitionSearches();

    void UpdateDefinitionSearchTopLevelStatus(IDefinitionSearch definitionSearch, bool isTopLevel, IAccount account);

    Task ValidateDefinitionSearch(IDefinitionSearch definitionSearch, IAccount account);

    public Task<IEnumerable<IDefinitionSearch>> GetAllDefinitionSearchesAsync(bool getTopLevelOnly);

    public Task RemoveSavedDefinitionSearch(IDefinitionSearch definitionSearch);
}
