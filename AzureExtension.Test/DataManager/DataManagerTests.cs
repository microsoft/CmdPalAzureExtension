// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Account;
using AzureExtension.Client;
using AzureExtension.Data;
using AzureExtension.DataManager;
using AzureExtension.DataModel;
using Moq;

namespace AzureExtension.Test.DataManager;

[TestClass]
public class DataManagerTests
{
    public DataStore GetTestDataStore()
    {
        var path = TestHelpers.GetUniqueFolderPath("AZT");
        var combinedPath = Path.Combine(path, "AzureData.db");
        var dataStoreSchema = new AzureDataStoreSchema();
        var dataStore = new DataStore("TestStore", combinedPath, dataStoreSchema);
        dataStore.Create();
        return dataStore;
    }

    public void CleanUpDataStore(DataStore dataStore)
    {
        var path = dataStore.DataStoreFilePath;
        dataStore.Dispose();

        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
        catch (IOException)
        {
            Directory.Delete(path, true);
        }
    }

    [TestMethod]
    public void TestManagerCreation()
    {
        var dataStore = GetTestDataStore();
        var stubAccountProvider = new Mock<IAccountProvider>().Object;
        var stubLiveDataProvider = new Mock<IAzureLiveDataProvider>().Object;
        var stubAuthProvider = new Mock<IAuthorizedEntityIdProvider>().Object;
        var queryManager = new AzureDataQueryManager(dataStore, stubAccountProvider, stubLiveDataProvider);
        var prsearchManager = new AzureDataPullRequestSearchManager(dataStore, stubAccountProvider, stubLiveDataProvider, stubAuthProvider);
        var azureDataManager = new AzureDataManager(dataStore, queryManager, prsearchManager);
        Assert.IsNotNull(azureDataManager);
        CleanUpDataStore(dataStore);
    }
}
