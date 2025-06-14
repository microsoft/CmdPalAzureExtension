// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Linq;
using AzureExtension.Account;
using AzureExtension.Client;
using AzureExtension.Controls;
using AzureExtension.Data;
using AzureExtension.DataManager;
using AzureExtension.DataModel;
using AzureExtension.PersistentData;
using Microsoft.Identity.Client;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.Policy.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Profile;
using Microsoft.VisualStudio.Services.WebApi;
using Moq;
using Serilog;
using PullRequestSearch = AzureExtension.DataModel.PullRequestSearch;
using Query = AzureExtension.DataModel.Query;
using TFModels = Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using WorkItem = AzureExtension.DataModel.WorkItem;

namespace AzureExtension.Test.DataManager;

[TestClass]
public class DataManagerTests
{
    public static DataStore GetTestDataStore()
    {
        var path = TestHelpers.GetUniqueFolderPath("AZT");
        var combinedPath = Path.Combine(path, "AzureData.db");
        var dataStoreSchema = new AzureCacheDataStoreSchema();
        var dataStore = new DataStore("TestStore", combinedPath, dataStoreSchema);
        dataStore.Create();
        return dataStore;
    }

    public static void CleanUpDataStore(DataStore dataStore)
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
        finally
        {
            Log.Information($"Cleaned up DataStore at {path}\n\n");
        }
    }

    [TestMethod]
    public void TestManagerCreation()
    {
        Log.Information("\nStarting TestManagerCreation");
        var dataStore = GetTestDataStore();
        var stubAccountProvider = new Mock<IAccountProvider>().Object;
        var stubLiveDataProvider = new Mock<IAzureLiveDataProvider>().Object;
        var stubAuthProvider = new Mock<IConnectionProvider>().Object;
        var stubConnectionProvider = new Mock<IConnectionProvider>().Object;
        var stubUpdateDictionary = new Dictionary<DataUpdateType, IDataUpdater>();
        var azureDataManager = new AzureDataManager(dataStore, stubUpdateDictionary);
        Assert.IsNotNull(azureDataManager);
        CleanUpDataStore(dataStore);
    }

    [TestMethod]
    public async Task TestDataQueryManagerUpdateFlow()
    {
        Log.Information("\nStarting TestDataQueryManagerUpdateFlow");
        var dataStore = GetTestDataStore();
        var mockAccountProvider = new Mock<IAccountProvider>();
        var mockLiveDataProvider = new Mock<IAzureLiveDataProvider>();
        var mockConnectionProvider = new Mock<IConnectionProvider>();
        var mockQueryRepository = new Mock<ISavedSearchesSource<IQuerySearch>>();
        var queryManager = new AzureDataQueryManager(dataStore, mockAccountProvider.Object, mockLiveDataProvider.Object, mockConnectionProvider.Object, mockQueryRepository.Object);

        var mockVssConnection = new Mock<IVssConnection>();
        var stubIdentity = new Microsoft.VisualStudio.Services.Identity.Identity()
        {
            Id = Guid.NewGuid(),
        };

        mockVssConnection.Setup(c => c.AuthorizedIdentity).Returns(stubIdentity);

        mockConnectionProvider
            .Setup(c => c.GetVssConnectionAsync(It.IsAny<Uri>(), It.IsAny<IAccount>()))
            .ReturnsAsync(mockVssConnection.Object);

        var stubAccount = new Mock<IAccount>();
        stubAccount.SetupGet(a => a.Username).Returns("TestUsername");

        mockAccountProvider.Setup(a => a.GetDefaultAccountAsync()).ReturnsAsync(stubAccount.Object);

        mockLiveDataProvider.Setup(p => p.GetTeamProject(It.IsAny<IVssConnection>(), It.IsAny<string>()))
            .ReturnsAsync(new TeamProject
            {
                Id = Guid.NewGuid(),
                Name = "Test Project",
                Url = "https://dev.azure.com/Org/Project",
            });

        mockLiveDataProvider.Setup(p => p.GetWorkItemQueryResultByIdAsync(It.IsAny<IVssConnection>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkItemQueryResult
            {
                QueryType = QueryType.Flat,
                WorkItems =
                [
                    new WorkItemReference
                    {
                        Id = 1,
                        Url = "https://dev.azure.com/Org/Project/_apis/wit/workitems/1",
                    },
                ],
            });

        mockLiveDataProvider.Setup(p => p.GetWorkItemsAsync(It.IsAny<IVssConnection>(), It.IsAny<string>(), It.IsAny<IEnumerable<int>>(), It.IsAny<WorkItemExpand>(), It.IsAny<WorkItemErrorPolicy>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new TFModels.WorkItem
            {
                    Id = 1,
                    Fields =
                    {
                        ["System.Title"] = "Test Work Item",
                        ["System.State"] = "New",
                        ["System.WorkItemType"] = "Task",
                    },
            },
            ]);

        mockLiveDataProvider.Setup(p => p.GetWorkItemTypeAsync(It.IsAny<IVssConnection>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TFModels.WorkItemType
            {
                Name = "Task",
                Url = "https://dev.azure.com/Org/Project/_apis/wit/workitemtypes/Task",
            });

        mockLiveDataProvider.Setup(p => p.GetAvatarAsync(It.IsAny<IVssConnection>(), It.IsAny<Guid>()))
            .ReturnsAsync(new Avatar
            {
                Value = Array.Empty<byte>(),
            });

        var testQuery = new Mock<IQuerySearch>();
        testQuery.SetupGet(q => q.Url).Returns("https://dev.azure.com/organization/project/_queries/query/12345678-1234-1234-1234-1234567890ab");
        testQuery.SetupGet(q => q.Name).Returns("Test Query");

        await queryManager.UpdateQueryAsync(testQuery.Object, CancellationToken.None);

        var dsQuery = Query.Get(dataStore, 1);

        Assert.IsNotNull(dsQuery);
        Assert.AreEqual("Test Query", dsQuery.Name);
        Assert.AreEqual("TestUsername", dsQuery.Username);
        Assert.AreEqual("Test Project", dsQuery.Project.Name);
        Assert.AreEqual("Test Work Item", WorkItem.GetForQuery(dataStore, dsQuery).First().SystemTitle);

        CleanUpDataStore(dataStore);
    }

    [TestMethod]
    public async Task TestDataQueryManagerUpdateFlow200Items()
    {
        Log.Information("\nStarting TestDataQueryManagerUpdateFlow200Items");
        var dataStore = GetTestDataStore();
        var mockAccountProvider = new Mock<IAccountProvider>();
        var mockLiveDataProvider = new Mock<IAzureLiveDataProvider>();
        var mockConnectionProvider = new Mock<IConnectionProvider>();
        var mockQueryRepository = new Mock<ISavedSearchesSource<IQuerySearch>>();
        var queryManager = new AzureDataQueryManager(dataStore, mockAccountProvider.Object, mockLiveDataProvider.Object, mockConnectionProvider.Object, mockQueryRepository.Object);

        var mockVssConnection = new Mock<IVssConnection>();
        var stubIdentity = new Microsoft.VisualStudio.Services.Identity.Identity()
        {
            Id = Guid.NewGuid(),
        };

        mockVssConnection.Setup(c => c.AuthorizedIdentity).Returns(stubIdentity);

        mockConnectionProvider
            .Setup(c => c.GetVssConnectionAsync(It.IsAny<Uri>(), It.IsAny<IAccount>()))
            .ReturnsAsync(mockVssConnection.Object);

        var stubAccount = new Mock<IAccount>();
        stubAccount.SetupGet(a => a.Username).Returns("TestUsername");

        mockAccountProvider.Setup(a => a.GetDefaultAccountAsync()).ReturnsAsync(stubAccount.Object);

        mockLiveDataProvider.Setup(p => p.GetTeamProject(It.IsAny<IVssConnection>(), It.IsAny<string>()))
            .ReturnsAsync(new TeamProject
            {
                Id = Guid.NewGuid(),
                Name = "Test Project",
                Url = "https://dev.azure.com/Org/Project",
            });

        // Generate 200 work item references
        var workItemRefs = Enumerable.Range(1, 200)
            .Select(id => new WorkItemReference
            {
                Id = id,
                Url = $"https://dev.azure.com/Org/Project/_apis/wit/workitems/{id}",
            })
            .ToList();

        mockLiveDataProvider.Setup(p => p.GetWorkItemQueryResultByIdAsync(It.IsAny<IVssConnection>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkItemQueryResult
            {
                QueryType = QueryType.Flat,
                WorkItems = workItemRefs,
            });

        // Generate 200 TFModels.WorkItem objects
        var tfWorkItems = Enumerable.Range(1, 200)
            .Select(id => new TFModels.WorkItem
            {
                Id = id,
                Fields =
                {
                    ["System.Title"] = $"Test Work Item {id}",
                    ["System.State"] = "New",
                    ["System.WorkItemType"] = "Task",
                },
            })
            .ToList();

        // The Azure API may request work items in batches, so match any input and return the corresponding items
        mockLiveDataProvider.Setup(p => p.GetWorkItemsAsync(
            It.IsAny<IVssConnection>(),
            It.IsAny<string>(),
            It.IsAny<IEnumerable<int>>(),
            It.IsAny<WorkItemExpand>(),
            It.IsAny<WorkItemErrorPolicy>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync((IVssConnection conn, string proj, IEnumerable<int> ids, WorkItemExpand exp, WorkItemErrorPolicy err, CancellationToken ct) =>
            tfWorkItems.Where(wi => ids.Contains(wi.Id!.Value)).ToList());

        mockLiveDataProvider.Setup(p => p.GetWorkItemTypeAsync(It.IsAny<IVssConnection>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TFModels.WorkItemType
            {
                Name = "Task",
                Url = "https://dev.azure.com/Org/Project/_apis/wit/workitemtypes/Task",
            });

        mockLiveDataProvider.Setup(p => p.GetAvatarAsync(It.IsAny<IVssConnection>(), It.IsAny<Guid>()))
            .ReturnsAsync(new Avatar
            {
                Value = Array.Empty<byte>(),
            });

        var testQuery = new Mock<IQuerySearch>();
        testQuery.SetupGet(q => q.Url).Returns("https://dev.azure.com/organization/project/_queries/query/12345678-1234-1234-1234-1234567890ab");
        testQuery.SetupGet(q => q.Name).Returns("Test Query");

        await queryManager.UpdateQueryAsync(testQuery.Object, CancellationToken.None);

        var dsQuery = Query.Get(dataStore, 1);

        Assert.IsNotNull(dsQuery);
        Assert.AreEqual("Test Query", dsQuery.Name);
        Assert.AreEqual("TestUsername", dsQuery.Username);
        Assert.AreEqual("Test Project", dsQuery.Project.Name);

        // Assert that all 200 work items are present and correct
        var storedWorkItems = WorkItem.GetForQuery(dataStore, dsQuery).ToList();
        Assert.AreEqual(200, storedWorkItems.Count);
        Assert.AreEqual("Test Work Item 1", storedWorkItems.First().SystemTitle);
        Assert.AreEqual("Test Work Item 200", storedWorkItems.Last().SystemTitle);

        CleanUpDataStore(dataStore);
    }

    [TestMethod]
    public async Task TestPullRequestSearchUpdateFlow()
    {
        Log.Information("Starting TestPullRequestSearchUpdateFlow");
        var dataStore = GetTestDataStore();
        var mockAccountProvider = new Mock<IAccountProvider>();
        var mockLiveDataProvider = new Mock<IAzureLiveDataProvider>();
        var mockConnectionProvider = new Mock<IConnectionProvider>();
        var mockPullRequestSearchRepository = new Mock<ISavedSearchesSource<IPullRequestSearch>>();
        var pullRequestSearchManager = new AzureDataPullRequestSearchManager(dataStore, mockAccountProvider.Object, mockLiveDataProvider.Object, mockConnectionProvider.Object, mockPullRequestSearchRepository.Object);

        var mockVssConnection = new Mock<IVssConnection>();
        var stubIdentity = new Microsoft.VisualStudio.Services.Identity.Identity()
        {
            Id = Guid.NewGuid(),
        };

        mockVssConnection.Setup(c => c.AuthorizedIdentity).Returns(stubIdentity);

        mockConnectionProvider
            .Setup(c => c.GetVssConnectionAsync(It.IsAny<Uri>(), It.IsAny<IAccount>()))
            .ReturnsAsync(mockVssConnection.Object);

        var stubAccount = new Mock<IAccount>();
        stubAccount.SetupGet(a => a.Username).Returns("TestUsername");

        mockAccountProvider.Setup(a => a.GetDefaultAccountAsync()).ReturnsAsync(stubAccount.Object);

        mockLiveDataProvider.Setup(p => p.GetTeamProject(It.IsAny<IVssConnection>(), It.IsAny<string>()))
            .ReturnsAsync(new TeamProject
            {
                Id = Guid.NewGuid(),
                Name = "Test Project",
                Url = "https://dev.azure.com/Org/Project",
            });

        mockLiveDataProvider.Setup(p => p.GetRepositoryAsync(It.IsAny<IVssConnection>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GitRepository
            {
                Id = Guid.NewGuid(),
                Name = "Test Repository",
                WebUrl = "https://dev.azure.com/Org/Project/_apis/git/repositories/TestRepository",
                ProjectReference = new TeamProjectReference
                {
                    Visibility = ProjectVisibility.Organization,
                },
            });

        mockLiveDataProvider.Setup(p => p.GetPullRequestsAsync(It.IsAny<IVssConnection>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<GitPullRequestSearchCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new GitPullRequest
                {
                    PullRequestId = 1,
                    Title = "Test Pull Request",
                    Url = "https://dev.azure.com/Org/Project/_apis/git/repositories/TestRepository/pullRequests/1",
                    Status = PullRequestStatus.Active,
                    TargetRefName = "refs/heads/main",
                    CreationDate = DateTime.UtcNow,
                    CreatedBy = new IdentityRef
                    {
                        DisplayName = "Test User",
                        Url = "https://dev.azure.com/Org/_apis/identities/12345678-1234-1234-1234-1234567890ab",
                    },
                },
            ]);

        mockLiveDataProvider.Setup(p => p.GetPolicyEvaluationsAsync(It.IsAny<IVssConnection>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new PolicyEvaluationRecord()
                {
                    Status = PolicyEvaluationStatus.Approved,
                    Configuration = new PolicyConfiguration()
                    {
                        IsEnabled = true,
                        IsBlocking = true,
                        Type = new PolicyType()
                        {
                            DisplayName = "Approved in test",
                        },
                    },
                }
            ]);

        var testPullRequestSearch = new Mock<IPullRequestSearch>();
        testPullRequestSearch.SetupGet(q => q.Url).Returns("https://dev.azure.com/organization/project/_pullRequests/12345678-1234-1234-1234-1234567890ab");
        testPullRequestSearch.SetupGet(q => q.Name).Returns("Test Pull Request Search");
        testPullRequestSearch.SetupGet(q => q.View).Returns("All");

        await pullRequestSearchManager.UpdatePullRequestsAsync(testPullRequestSearch.Object, CancellationToken.None);

        var dsPullRequestSearch = PullRequestSearch.Get(dataStore, 1);
        Assert.IsNotNull(dsPullRequestSearch);
        Assert.AreEqual("TestUsername", dsPullRequestSearch.Username);
        Assert.AreEqual("Test Project", dsPullRequestSearch.Project.Name);
        Assert.AreEqual("Test Repository", dsPullRequestSearch.Repository.Name);

        var dsPullRequest = PullRequest.GetForPullRequestSearch(dataStore, dsPullRequestSearch).First();
        Assert.AreEqual("Test Pull Request", dsPullRequest.Title);
        Assert.AreEqual("Approved", dsPullRequest.PolicyStatus);
        Assert.AreEqual("Approved in test", dsPullRequest.PolicyStatusReason);
        CleanUpDataStore(dataStore);
    }
}
