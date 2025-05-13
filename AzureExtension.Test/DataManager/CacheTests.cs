// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls;
using AzureExtension.DataManager;
using AzureExtension.DataManager.Cache;
using Moq;

namespace AzureExtension.Test.DataManager;

[TestClass]
public class CacheTests
{
    [TestMethod]
    public void TestCacheManagerInitialization()
    {
        var dataUpdateService = new Mock<IDataUpdateService>();
        using var cacheManager = new CacheManager(dataUpdateService.Object);

        var state = cacheManager.State;

        Assert.IsNotNull(state);
        Assert.AreEqual(cacheManager.IdleState, state);
    }

    [TestMethod]
    public async Task TestCacheManagerPeriodicUpdate()
    {
        var dataUpdateService = new Mock<IDataUpdateService>();
        using var cacheManager = new CacheManager(dataUpdateService.Object);

        Assert.AreEqual(cacheManager.IdleState, cacheManager.State);

        // cacheManager.Start();
        await cacheManager.PeriodicUpdate();

        Assert.AreEqual(cacheManager.PeriodicUpdatingState, cacheManager.State);

        dataUpdateService.Raise(x => x.OnUpdate += null, new DataManagerUpdateEventArgs(
            DataManagerUpdateKind.Success,
            new DataUpdateParameters() { UpdateType = DataUpdateType.All }));

        Assert.AreEqual(cacheManager.IdleState, cacheManager.State);
    }

    [TestMethod]
    public async Task TestCacheManagerRefresh()
    {
        var dataUpdateService = new Mock<IDataUpdateService>();
        using var cacheManager = new CacheManager(dataUpdateService.Object);

        Assert.AreEqual(cacheManager.IdleState, cacheManager.State);

        var stubQuery = new Mock<IQuerySearch>();

        await cacheManager.Refresh(new DataUpdateParameters() { UpdateType = DataUpdateType.Query, UpdateObject = stubQuery.Object });

        Assert.AreEqual(cacheManager.RefreshingState, cacheManager.State);

        dataUpdateService.Raise(x => x.OnUpdate += null, new DataManagerUpdateEventArgs(
            DataManagerUpdateKind.Success,
            new DataUpdateParameters() { UpdateType = DataUpdateType.Query, UpdateObject = stubQuery.Object }));

        Assert.AreEqual(cacheManager.IdleState, cacheManager.State);
    }

    [TestMethod]
    public async Task TestCacheManagerRefreshWithError()
    {
        var dataUpdateService = new Mock<IDataUpdateService>();
        using var cacheManager = new CacheManager(dataUpdateService.Object);

        Assert.AreEqual(cacheManager.IdleState, cacheManager.State);

        var stubQuery = new Mock<IQuerySearch>();
        await cacheManager.Refresh(new DataUpdateParameters() { UpdateType = DataUpdateType.Query, UpdateObject = stubQuery.Object });

        Assert.AreEqual(cacheManager.RefreshingState, cacheManager.State);

        dataUpdateService.Raise(x => x.OnUpdate += null, new DataManagerUpdateEventArgs(
            DataManagerUpdateKind.Error,
            new DataUpdateParameters() { UpdateType = DataUpdateType.Query, UpdateObject = stubQuery.Object },
            new InvalidOperationException("Test exception")));

        Assert.AreEqual(cacheManager.IdleState, cacheManager.State);
    }

    [TestMethod]
    public async Task TestCacheManagerRefreshWhileRefreshingSameQuery()
    {
        var dataUpdateService = new Mock<IDataUpdateService>();
        using var cacheManager = new CacheManager(dataUpdateService.Object);

        Assert.AreEqual(cacheManager.IdleState, cacheManager.State);

        var stubQuery = new Mock<IQuerySearch>();
        stubQuery.SetupGet(x => x.Name).Returns("Query 1");
        stubQuery.SetupGet(x => x.Url).Returns("testUrl");

        await cacheManager.Refresh(new DataUpdateParameters() { UpdateType = DataUpdateType.Query, UpdateObject = stubQuery.Object });

        Assert.AreEqual(cacheManager.RefreshingState, cacheManager.State);
        Assert.IsNotNull(cacheManager.CurrentUpdateParameters);
        Assert.AreEqual(stubQuery.Object, cacheManager.CurrentUpdateParameters.UpdateObject);

        // Should be ignored
        await cacheManager.Refresh(new DataUpdateParameters() { UpdateType = DataUpdateType.Query, UpdateObject = stubQuery.Object });

        Assert.AreEqual(cacheManager.RefreshingState, cacheManager.State);
        Assert.IsNotNull(cacheManager.CurrentUpdateParameters);
        Assert.AreEqual(stubQuery.Object, cacheManager.CurrentUpdateParameters.UpdateObject);
    }

    [TestMethod]
    public async Task TestCacheManagerRefreshWhileRefreshingDifferentQuery()
    {
        var dataUpdateService = new Mock<IDataUpdateService>();
        using var cacheManager = new CacheManager(dataUpdateService.Object);

        Assert.AreEqual(cacheManager.IdleState, cacheManager.State);

        var stubQuery1 = new Mock<IQuerySearch>();
        stubQuery1.SetupGet(x => x.Name).Returns("Query 1");
        stubQuery1.SetupGet(x => x.Url).Returns("testUrl1");
        var stubQuery2 = new Mock<IQuerySearch>();
        stubQuery2.SetupGet(x => x.Name).Returns("Query 2");
        stubQuery2.SetupGet(x => x.Url).Returns("testUrl2");

        await cacheManager.Refresh(new DataUpdateParameters() { UpdateType = DataUpdateType.Query, UpdateObject = stubQuery1.Object });
        Assert.AreEqual(cacheManager.RefreshingState, cacheManager.State);
        Assert.IsNotNull(cacheManager.CurrentUpdateParameters);
        Assert.AreEqual(stubQuery1.Object, cacheManager.CurrentUpdateParameters.UpdateObject);

        await cacheManager.Refresh(new DataUpdateParameters() { UpdateType = DataUpdateType.Query, UpdateObject = stubQuery2.Object });
        Assert.AreEqual(cacheManager.PendingRefreshState, cacheManager.State);
        Assert.IsNotNull(cacheManager.CurrentUpdateParameters);
        Assert.AreEqual(stubQuery2.Object, cacheManager.CurrentUpdateParameters.UpdateObject);

        dataUpdateService.Raise(x => x.OnUpdate += null, new DataManagerUpdateEventArgs(
            DataManagerUpdateKind.Cancel,
            new DataUpdateParameters() { UpdateType = DataUpdateType.Query, UpdateObject = stubQuery1.Object }));

        Assert.AreEqual(cacheManager.RefreshingState, cacheManager.State);
        Assert.IsNotNull(cacheManager.CurrentUpdateParameters);
        Assert.AreEqual(stubQuery2.Object, cacheManager.CurrentUpdateParameters.UpdateObject);

        dataUpdateService.Raise(x => x.OnUpdate += null, new DataManagerUpdateEventArgs(
            DataManagerUpdateKind.Success,
            new DataUpdateParameters() { UpdateType = DataUpdateType.Query, UpdateObject = stubQuery1.Object }));

        Assert.AreEqual(cacheManager.IdleState, cacheManager.State);
        Assert.IsNull(cacheManager.CurrentUpdateParameters);
    }

    [TestMethod]
    public async Task TestCacheManagerRefreshWhilePeriodicUpdating()
    {
        var dataUpdateService = new Mock<IDataUpdateService>();
        using var cacheManager = new CacheManager(dataUpdateService.Object);

        Assert.AreEqual(cacheManager.IdleState, cacheManager.State);

        var stubQuery = new Mock<IQuerySearch>();
        stubQuery.SetupGet(x => x.Name).Returns("Query 1");
        stubQuery.SetupGet(x => x.Url).Returns("testUrl");

        await cacheManager.PeriodicUpdate();
        Assert.AreEqual(cacheManager.PeriodicUpdatingState, cacheManager.State);
        Assert.IsNull(cacheManager.CurrentUpdateParameters);

        await cacheManager.Refresh(new DataUpdateParameters() { UpdateType = DataUpdateType.Query, UpdateObject = stubQuery.Object });
        Assert.AreEqual(cacheManager.PendingRefreshState, cacheManager.State);
        Assert.IsNotNull(cacheManager.CurrentUpdateParameters);
        Assert.AreEqual(stubQuery.Object, cacheManager.CurrentUpdateParameters.UpdateObject);

        dataUpdateService.Raise(x => x.OnUpdate += null, new DataManagerUpdateEventArgs(
            DataManagerUpdateKind.Cancel,
            new DataUpdateParameters() { UpdateType = DataUpdateType.Query, UpdateObject = stubQuery.Object }));

        Assert.AreEqual(cacheManager.RefreshingState, cacheManager.State);
        Assert.IsNotNull(cacheManager.CurrentUpdateParameters);
        Assert.AreEqual(stubQuery.Object, cacheManager.CurrentUpdateParameters.UpdateObject);

        dataUpdateService.Raise(x => x.OnUpdate += null, new DataManagerUpdateEventArgs(
            DataManagerUpdateKind.Success,
            new DataUpdateParameters() { UpdateType = DataUpdateType.Query, UpdateObject = stubQuery.Object }));

        Assert.AreEqual(cacheManager.IdleState, cacheManager.State);
        Assert.IsNull(cacheManager.CurrentUpdateParameters);
    }

    [TestMethod]
    public async Task TestCacheManagerPeriodicUpdateWhileRefreshing()
    {
        var dataUpdateService = new Mock<IDataUpdateService>();
        using var cacheManager = new CacheManager(dataUpdateService.Object);

        Assert.AreEqual(cacheManager.IdleState, cacheManager.State);

        var stubQuery = new Mock<IQuerySearch>();
        stubQuery.SetupGet(x => x.Name).Returns("Query 1");
        stubQuery.SetupGet(x => x.Url).Returns("testUrl");

        await cacheManager.Refresh(new DataUpdateParameters() { UpdateType = DataUpdateType.Query, UpdateObject = stubQuery.Object });

        Assert.AreEqual(cacheManager.RefreshingState, cacheManager.State);
        Assert.IsNotNull(cacheManager.CurrentUpdateParameters);
        Assert.AreEqual(stubQuery.Object, cacheManager.CurrentUpdateParameters.UpdateObject);

        // Should be ignored
        await cacheManager.PeriodicUpdate();

        Assert.AreEqual(cacheManager.RefreshingState, cacheManager.State);
        Assert.IsNotNull(cacheManager.CurrentUpdateParameters);
        Assert.AreEqual(stubQuery.Object, cacheManager.CurrentUpdateParameters.UpdateObject);
   }
}
