// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls;
using Query = AzureExtension.DataModel.Query;

namespace AzureExtension.DataManager;

public interface IDataObjectProvider
{
    Query? GetQuery(IQuery query);

    IEnumerable<IWorkItem> GetWorkItems(IQuery query);
}
