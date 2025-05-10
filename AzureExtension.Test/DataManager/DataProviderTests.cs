// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls;
using AzureExtension.DataManager;
using AzureExtension.DataManager.Cache;
using AzureExtension.DataModel;
using Moq;
using PullRequestSearch = AzureExtension.DataModel.PullRequestSearch;
using Query = AzureExtension.DataModel.Query;
using WorkItem = AzureExtension.DataModel.WorkItem;

namespace AzureExtension.Test.DataManager;

[TestClass]
public class DataProviderTests
{
    [TestMethod]
    public void DataProviderCreate()
    {
        var stubContentDataDictionary = new Dictionary<Type, IContentDataProvider>();
        var stubSearchDataDictionary = new Dictionary<Type, ISearchDataProvider>();
        var dataProvider = new LiveDataProvider(
            new Mock<ICacheManager>().Object,
            stubContentDataDictionary,
            stubSearchDataDictionary);

        Assert.IsNotNull(dataProvider);
    }

    [TestMethod]
    public async Task DataProviderGetWorkItemsFromFreshQuery()
    {
        var mockCacheManager = new Mock<ICacheManager>();
        var mockQueryProvider = new Mock<ISearchDataProvider>();
        var mockQueryContentProvider = new Mock<IContentDataProvider>();
        var stubContentDataDictionary = new Dictionary<Type, IContentDataProvider>
        {
            { typeof(IQuery), mockQueryContentProvider.Object },
        };

        var stubSearchDataDictionary = new Dictionary<Type, ISearchDataProvider>
        {
            { typeof(IQuery), mockQueryProvider.Object },
        };

        var dataProvider = new LiveDataProvider(
            mockCacheManager.Object,
            stubContentDataDictionary,
            stubSearchDataDictionary);

        var stubQuery = new Mock<IQuery>();

        var getWorkItemsTask = dataProvider.GetContentData<IWorkItem>(stubQuery.Object);

        mockCacheManager.Verify(m => m.RequestRefresh(It.IsAny<DataUpdateParameters>()), Times.Once);
        mockQueryContentProvider.Verify(m => m.GetDataObjects(It.IsAny<IQuery>()), Times.Never);
        mockCacheManager.Raise(m => m.OnUpdate += null, new CacheManagerUpdateEventArgs(CacheManagerUpdateKind.Updated));
        await getWorkItemsTask;

        mockQueryContentProvider.Verify(m => m.GetDataObjects(It.IsAny<IQuery>()), Times.Once);
    }

    [TestMethod]
    public async Task DataProviderGetWorkItemsFromCachedQuery()
    {
        var mockCacheManager = new Mock<ICacheManager>();
        var mockQueryProvider = new Mock<ISearchDataProvider>();
        var mockQueryContentProvider = new Mock<IContentDataProvider>();
        var stubContentDataDictionary = new Dictionary<Type, IContentDataProvider>
        {
            { typeof(IQuery), mockQueryContentProvider.Object },
        };

        var stubSearchDataDictionary = new Dictionary<Type, ISearchDataProvider>
        {
            { typeof(IQuery), mockQueryProvider.Object },
        };

        var dataProvider = new LiveDataProvider(
            mockCacheManager.Object,
            stubContentDataDictionary,
            stubSearchDataDictionary);

        var stubQuery = new Mock<IQuery>();

        var dsQuery = new Query();

        mockQueryProvider
            .Setup(m => m.GetDataForSearch(It.IsAny<IQuery>()))
            .Returns(dsQuery);

        var getWorkItemsTask = dataProvider.GetContentData<IWorkItem>(stubQuery.Object);

        mockCacheManager.Verify(m => m.RequestRefresh(It.IsAny<DataUpdateParameters>()), Times.Once);
        mockQueryContentProvider.Verify(m => m.GetDataObjects(It.IsAny<IQuery>()), Times.Once);
        mockCacheManager.Raise(m => m.OnUpdate += null, new CacheManagerUpdateEventArgs(CacheManagerUpdateKind.Updated));
        await getWorkItemsTask;

        mockQueryContentProvider.Verify(m => m.GetDataObjects(It.IsAny<IQuery>()), Times.Once);
    }

    [TestMethod]
    public async Task DataProviderDefinitionFromFreshDefinition()
    {
        var mockCacheManager = new Mock<ICacheManager>();
        var mockPipelineProvider = new Mock<ISearchDataProvider>();
        var mockPipelineContentProvider = new Mock<IContentDataProvider>();
        var stubContentDataDictionary = new Dictionary<Type, IContentDataProvider>()
        {
            { typeof(IPipelineDefinitionSearch), mockPipelineContentProvider.Object },
        };
        var stubSearchDataDictionary = new Dictionary<Type, ISearchDataProvider>
        {
            { typeof(IPipelineDefinitionSearch), mockPipelineProvider.Object },
        };

        var dataProvider = new LiveDataProvider(
            mockCacheManager.Object,
            stubContentDataDictionary,
            stubSearchDataDictionary);

        var stubDefinitionSearch = new Mock<IPipelineDefinitionSearch>();

        var getDefinitionTask = dataProvider.GetSearchData<IDefinition>(stubDefinitionSearch.Object);

        mockCacheManager.Verify(m => m.RequestRefresh(It.IsAny<DataUpdateParameters>()), Times.Once);
        mockPipelineProvider.Verify(m => m.GetDataForSearch(It.IsAny<IPipelineDefinitionSearch>()), Times.Once);
        mockCacheManager.Raise(m => m.OnUpdate += null, new CacheManagerUpdateEventArgs(CacheManagerUpdateKind.Updated));

        await getDefinitionTask;

        mockPipelineProvider.Verify(m => m.GetDataForSearch(It.IsAny<IPipelineDefinitionSearch>()), Times.Exactly(2));
    }
}
