// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls;
using AzureExtension.Data;
using AzureExtension.DataModel;

namespace AzureExtension.DataManager;

public class AzureDataPipelineProvider : IPipelineProvider
{
    private readonly DataStore _dataStore;

    public AzureDataPipelineProvider(DataStore dataStore)
    {
        _dataStore = dataStore;
    }

    public Definition? GetDefinition(IDefinitionSearch definitionSearch)
    {
         return Definition.GetByInternalId(_dataStore, definitionSearch.InternalId);
    }

    public IEnumerable<IBuild> GetBuilds(IDefinitionSearch definitionSearch)
    {
        var dsDefinition = GetDefinition(definitionSearch);
        if (dsDefinition is null)
        {
            return Enumerable.Empty<IBuild>();
        }

        return Build.GetForDefinition(_dataStore, dsDefinition.Id);
    }
}
