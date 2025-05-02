// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls;
using AzureExtension.DataManager;
using AzureExtension.DataManager.Cache;
using Moq;

namespace AzureExtension.Test.DataManager;

[TestClass]
public class DataProviderTests
{
    [TestMethod]
    public void DataProviderCreate()
    {
        var dataProvider = new DataProvider(
            new Mock<ICacheManager>().Object,
            new Mock<IDataQueryProvider>().Object,
            new Mock<IDataPullRequestSearchProvider>().Object,
            new Mock<IPipelineProvider>().Object);

        Assert.IsNotNull(dataProvider);
    }

    [TestMethod]
    public async Task DataProviderGetWorkItemsFromFreshQuery()
    {
        var mockCacheManager = new Mock<ICacheManager>();
        var mockQueryProvider = new Mock<IDataQueryProvider>();
        var stubPullRequestSearchProvider = new Mock<IDataPullRequestSearchProvider>();
        var stubPipelineProvider = new Mock<IPipelineProvider>();
        var dataProvider = new DataProvider(
            mockCacheManager.Object,
            mockQueryProvider.Object,
            stubPullRequestSearchProvider.Object,
            stubPipelineProvider.Object);

        var stubQuery = new Mock<IQuery>();

        var getWorkItemsTask = dataProvider.GetWorkItems(stubQuery.Object);

        mockCacheManager.Verify(m => m.RequestRefresh(It.IsAny<DataUpdateParameters>()), Times.Once);
        mockCacheManager.Raise(m => m.OnUpdate += null, new CacheManagerUpdateEventArgs(CacheManagerUpdateKind.Updated));

        await getWorkItemsTask;

        mockQueryProvider.Verify(m => m.GetWorkItems(It.IsAny<IQuery>()), Times.Once);
    }

    [TestMethod]
    public async Task DataProviderGetWorkItemsFromCachedQuery()
    {
        var mockCacheManager = new Mock<ICacheManager>();
        var mockQueryProvider = new Mock<IDataQueryProvider>();
        var stubPullRequestSearchProvider = new Mock<IDataPullRequestSearchProvider>();
        var stubPipelineProvider = new Mock<IPipelineProvider>();
        var dataProvider = new DataProvider(
            mockCacheManager.Object,
            mockQueryProvider.Object,
            stubPullRequestSearchProvider.Object,
            stubPipelineProvider.Object);

        var stubQuery = new Mock<IQuery>();

        var dsQuery = new DataModel.Query();

        mockQueryProvider
            .Setup(m => m.GetQuery(It.IsAny<IQuery>()))
            .Returns(dsQuery);

        var getWorkItemsTask = dataProvider.GetWorkItems(stubQuery.Object);

        mockCacheManager.Verify(m => m.RequestRefresh(It.IsAny<DataUpdateParameters>()), Times.Once);
        mockCacheManager.Raise(m => m.OnUpdate += null, new CacheManagerUpdateEventArgs(CacheManagerUpdateKind.Updated));

        await getWorkItemsTask;

        mockQueryProvider.Verify(m => m.GetWorkItems(It.IsAny<IQuery>()), Times.Once);
    }

    [TestMethod]
    public async Task DataProviderPullRequestsFromFreshPullRequestSearch()
    {
        var mockCacheManager = new Mock<ICacheManager>();
        var stubQueryProvider = new Mock<IDataQueryProvider>();
        var mockPullRequestSearchProvider = new Mock<IDataPullRequestSearchProvider>();
        var stubPipelineProvider = new Mock<IPipelineProvider>();
        var dataProvider = new DataProvider(
            mockCacheManager.Object,
            stubQueryProvider.Object,
            mockPullRequestSearchProvider.Object,
            stubPipelineProvider.Object);

        var stubPullRequestSearch = new Mock<IPullRequestSearch>();

        var getPullRequestsTask = dataProvider.GetPullRequests(stubPullRequestSearch.Object);

        mockCacheManager.Verify(m => m.RequestRefresh(It.IsAny<DataUpdateParameters>()), Times.Once);
        mockCacheManager.Raise(m => m.OnUpdate += null, new CacheManagerUpdateEventArgs(CacheManagerUpdateKind.Updated));

        await getPullRequestsTask;

        mockPullRequestSearchProvider.Verify(m => m.GetPullRequests(It.IsAny<IPullRequestSearch>()), Times.Once);
    }

    [TestMethod]
    public async Task DataProviderPullRequestsFromCachedPullRequestSearch()
    {
        var mockCacheManager = new Mock<ICacheManager>();
        var stubQueryProvider = new Mock<IDataQueryProvider>();
        var mockPullRequestSearchProvider = new Mock<IDataPullRequestSearchProvider>();
        var stubPipelineProvider = new Mock<IPipelineProvider>();
        var dataProvider = new DataProvider(
            mockCacheManager.Object,
            stubQueryProvider.Object,
            mockPullRequestSearchProvider.Object,
            stubPipelineProvider.Object);

        var stubPullRequestSearch = new Mock<IPullRequestSearch>();

        var dsPullRequestSearch = new DataModel.PullRequestSearch();
        mockPullRequestSearchProvider
            .Setup(m => m.GetPullRequestSearch(It.IsAny<IPullRequestSearch>()))
            .Returns(dsPullRequestSearch);

        var getPullRequestsTask = dataProvider.GetPullRequests(stubPullRequestSearch.Object);

        mockCacheManager.Verify(m => m.RequestRefresh(It.IsAny<DataUpdateParameters>()), Times.Once);
        mockCacheManager.Raise(m => m.OnUpdate += null, new CacheManagerUpdateEventArgs(CacheManagerUpdateKind.Updated));

        await getPullRequestsTask;

        mockPullRequestSearchProvider.Verify(m => m.GetPullRequests(It.IsAny<IPullRequestSearch>()), Times.Once);
    }
}
