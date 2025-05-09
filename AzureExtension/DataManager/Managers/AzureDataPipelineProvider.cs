// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls;
using AzureExtension.Data;
using AzureExtension.DataModel;

namespace AzureExtension.DataManager;

public class AzureDataPipelineProvider : IDataProvider<IPipelineDefinitionSearch, Definition, Build>
{
    private readonly DataStore _dataStore;

    public AzureDataPipelineProvider(DataStore dataStore)
    {
        _dataStore = dataStore;
    }

    public Definition? GetDataForSearch(IPipelineDefinitionSearch definitionSearch)
    {
         return Definition.GetByInternalId(_dataStore, definitionSearch.InternalId);
    }

    public IEnumerable<Build> GetDataObjects(IPipelineDefinitionSearch definitionSearch)
    {
        var dsDefinition = GetDataForSearch(definitionSearch);
        if (dsDefinition is null)
        {
            return Enumerable.Empty<Build>();
        }

        return Build.GetForDefinition(_dataStore, dsDefinition.Id);
    }
}
