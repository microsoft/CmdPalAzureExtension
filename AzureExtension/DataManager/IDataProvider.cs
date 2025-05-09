// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls;

namespace AzureExtension.DataManager;

#pragma warning disable SA1649 // File name should match first type name
public interface IDataProvider<TDataSearch, TDataResult, TDataObject>
#pragma warning restore SA1649 // File name should match first type name
    where TDataSearch : IAzureSearch
{
    TDataResult? GetDataForSearch(TDataSearch search);

    IEnumerable<TDataObject> GetDataObjects(TDataSearch search);
}

public interface IDataProvider
{
    object? GetDataForSearch(IAzureSearch search);

    IEnumerable<object> GetDataObjects(IAzureSearch search);
}

public class DataProviderAdapter<TDataSearch, TDataResult, TDataObject> : IDataProvider
    where TDataSearch : IAzureSearch
{
    private readonly IDataProvider<TDataSearch, TDataResult, TDataObject> _dataProvider;

    public DataProviderAdapter(IDataProvider<TDataSearch, TDataResult, TDataObject> dataProvider)
    {
        _dataProvider = dataProvider;
    }

    public object? GetDataForSearch(IAzureSearch search)
    {
        if (search is not TDataSearch typedSearch)
        {
            throw new ArgumentException($"Invalid search type: {search.GetType().Name}");
        }

        return _dataProvider.GetDataForSearch(typedSearch);
    }

    public IEnumerable<object> GetDataObjects(IAzureSearch search)
    {
        if (search is not TDataSearch typedSearch)
        {
            throw new ArgumentException($"Invalid search type: {search.GetType().Name}");
        }

        return _dataProvider.GetDataObjects(typedSearch).Cast<object>();
    }
}
