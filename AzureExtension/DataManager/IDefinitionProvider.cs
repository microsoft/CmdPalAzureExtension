// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls;
using AzureExtension.DataModel;

namespace AzureExtension.DataManager;

public interface IDefinitionProvider
{
    Definition? GetDefinition(IPipelineDefinitionSearch definitionSearch);

    IEnumerable<IBuild> GetBuilds(IPipelineDefinitionSearch definitionSearch);
}
