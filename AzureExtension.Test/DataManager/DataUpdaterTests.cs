// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.DataManager;
using Moq;

namespace AzureExtension.Test.DataManager;

[TestClass]
public class DataUpdaterTests
{
    [TestMethod]
    public async Task TestUpdateSpecificType()
    {
        var testDataStore = DataManagerTests.GetTestDataStore();
        var mockDataUpdater = new Mock<IDataUpdater>();
        var stubDictionary = new Dictionary<DataUpdateType, IDataUpdater>
        {
            { DataUpdateType.PullRequests, mockDataUpdater.Object },
        };

        var azureDataManager = new AzureDataManager(testDataStore, stubDictionary);

        var parameters = new DataUpdateParameters
        {
            UpdateType = DataUpdateType.PullRequests,
            CancellationToken = CancellationToken.None,
        };

        await azureDataManager.UpdateData(parameters);

        mockDataUpdater.Verify(x => x.UpdateData(It.Is<DataUpdateParameters>(p => p.UpdateType == DataUpdateType.PullRequests)), Times.Once);
        mockDataUpdater.Verify(x => x.PruneObsoleteData(), Times.Once);
        DataManagerTests.CleanUpDataStore(testDataStore);
    }

    [TestMethod]
    public async Task TestUpdateAllTypes()
    {
        var testDataStore = DataManagerTests.GetTestDataStore();
        var mockDataUpdater = new Mock<IDataUpdater>();
        var stubDictionary = new Dictionary<DataUpdateType, IDataUpdater>
        {
            { DataUpdateType.PullRequests, mockDataUpdater.Object },
            { DataUpdateType.Query, mockDataUpdater.Object },
            { DataUpdateType.Pipeline, mockDataUpdater.Object },
        };
        var azureDataManager = new AzureDataManager(testDataStore, stubDictionary);
        var parameters = new DataUpdateParameters
        {
            UpdateType = DataUpdateType.All,
            CancellationToken = CancellationToken.None,
        };
        await azureDataManager.UpdateData(parameters);
        mockDataUpdater.Verify(x => x.UpdateData(It.Is<DataUpdateParameters>(p => p.UpdateType == DataUpdateType.All)), Times.Exactly(3));
        mockDataUpdater.Verify(x => x.PruneObsoleteData(), Times.Exactly(3));
        DataManagerTests.CleanUpDataStore(testDataStore);
    }

    [TestMethod]
    public async Task TestUpdateWithCancellation()
    {
        var testDataStore = DataManagerTests.GetTestDataStore();
        var mockDataUpdater = new Mock<IDataUpdater>();
        mockDataUpdater.Setup(x => x.UpdateData(It.IsAny<DataUpdateParameters>()))
            .Throws(new OperationCanceledException("Operation was canceled."));
        var stubDictionary = new Dictionary<DataUpdateType, IDataUpdater>
        {
            { DataUpdateType.PullRequests, mockDataUpdater.Object },
        };
        var azureDataManager = new AzureDataManager(testDataStore, stubDictionary);
        var parameters = new DataUpdateParameters
        {
            UpdateType = DataUpdateType.PullRequests,
            CancellationToken = new CancellationToken(true),
        };
        await azureDataManager.UpdateData(parameters);
        mockDataUpdater.Verify(x => x.UpdateData(It.Is<DataUpdateParameters>(p => p.UpdateType == DataUpdateType.PullRequests)), Times.Once);
        mockDataUpdater.Verify(x => x.PruneObsoleteData(), Times.Never);
        DataManagerTests.CleanUpDataStore(testDataStore);
    }
}
