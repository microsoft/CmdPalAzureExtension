// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls;
using AzureExtension.Data;
using AzureExtension.DataModel;
using Serilog;

namespace AzureExtension.DataManager;

public class DataProvider : IDataProvider
{
    private readonly ILogger _log;
    private readonly AzureDataManager _cache;
    private readonly DataStore _dataStore;

    public DataProvider(AzureDataManager cache, DataStore dataStore)
    {
        _log = Log.ForContext("SourceContext", nameof(IDataProvider));
        _cache = cache;
        _dataStore = dataStore;
    }

    public async Task<IEnumerable<IWorkItem>> GetWorkItems(IQuery query)
    {
        var dsQuery = _cache.GetQuery(query);
        if (dsQuery == null)
        {
            await _cache.UpdateWorkItems(query);
        }

        dsQuery = _cache.GetQuery(query);

        return WorkItem.GetForQuery(_dataStore, dsQuery!);
    }
}
