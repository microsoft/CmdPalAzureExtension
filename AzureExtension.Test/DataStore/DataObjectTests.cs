﻿// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Data;
using AzureExtension.DataModel;
using AzureExtension.Helpers;
using Dapper.Contrib.Extensions;

namespace AzureExtension.Test;

public partial class DataStoreTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public void DateTimeExtension()
    {
        var now = DateTime.UtcNow;
        TestContext?.WriteLine($"Now: {now}");
        var nowAsInteger = now.ToDataStoreInteger();
        TestContext?.WriteLine($"NowAsDataStoreInteger: {nowAsInteger}");
        var nowFromInteger = nowAsInteger.ToDateTime();
        TestContext?.WriteLine($"NowFromDataStoreInteger: {nowFromInteger}");

        // We should not lose precision in the conversion to/from datastore format.
        Assert.AreEqual(now, nowFromInteger);
        Assert.AreEqual(now, now.ToDataStoreInteger().ToDateTime());
        Assert.AreEqual(now, now.ToDataStoreString().ToDateTime());

        // Working with the value should be as easy as working with dates, converting to numbers,
        // and using them in queries.
        var thirtyDays = new TimeSpan(30, 0, 0);
        TestContext?.WriteLine($"ThirtyDays: {thirtyDays}");
        var thirtyDaysAgo = now.Subtract(thirtyDays);
        TestContext?.WriteLine($"ThirtyDaysAgo: {thirtyDaysAgo}");
        var thirtyDaysAgoAsInteger = thirtyDaysAgo.ToDataStoreInteger();
        TestContext?.WriteLine($"ThirtyDaysAgoAsInteger: {thirtyDaysAgoAsInteger}");
        TestContext?.WriteLine($"ThirtyDays Ticks: {thirtyDays.Ticks}");
        TestContext?.WriteLine($"IntegerDiff: {nowAsInteger - thirtyDaysAgoAsInteger}");

        // Doing some timespan manipulation should still result in the same tick difference.
        // Also verify TimeSpan converters.
        Assert.AreEqual(thirtyDays.Ticks, nowAsInteger - thirtyDaysAgoAsInteger);
        Assert.AreEqual(thirtyDays, thirtyDays.ToDataStoreInteger().ToTimeSpan());
        Assert.AreEqual(thirtyDays, thirtyDays.ToDataStoreString().ToTimeSpan());

        // Test adding metadata time as string to the datastore.
        using var dataStore = new DataStore("TestStore", TestHelpers.GetDataStoreFilePath(TestOptions), TestOptions.DataStoreOptions.DataStoreSchema!);
        Assert.IsNotNull(dataStore);
        dataStore.Create();
        Assert.IsNotNull(dataStore.Connection);
        MetaData.AddOrUpdate(dataStore, "Now", now.ToDataStoreString());
        MetaData.AddOrUpdate(dataStore, "ThirtyDays", thirtyDays.ToDataStoreString());
        var nowFromMetaData = MetaData.Get(dataStore, "Now");
        Assert.IsNotNull(nowFromMetaData);
        var thirtyDaysFromMetaData = MetaData.Get(dataStore, "ThirtyDays");
        Assert.IsNotNull(thirtyDaysFromMetaData);
        Assert.AreEqual(now, nowFromMetaData.ToDateTime());
        Assert.AreEqual(thirtyDays, thirtyDaysFromMetaData.ToTimeSpan());
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void ReadAndWriteMetaData()
    {
        using var dataStore = new DataStore("TestStore", TestHelpers.GetDataStoreFilePath(TestOptions), TestOptions.DataStoreOptions.DataStoreSchema!);
        Assert.IsNotNull(dataStore);
        dataStore.Create();
        Assert.IsNotNull(dataStore.Connection);

        var metadata = new List<MetaData>
        {
            { new MetaData { Key = "Kittens", Value = "Cute" } },
            { new MetaData { Key = "Puppies", Value = "LotsOfWork" } },
        };

        using var tx = dataStore.Connection!.BeginTransaction();
        dataStore.Connection.Insert(metadata[0]);
        dataStore.Connection.Insert(metadata[1]);
        tx.Commit();

        // Verify retrieval and input into data objects.
        var dataStoreMetaData = dataStore.Connection.GetAll<MetaData>().ToList();
        Assert.AreEqual(dataStoreMetaData.Count, 2);
        foreach (var metaData in dataStoreMetaData)
        {
            TestContext?.WriteLine($"  Id: {metaData.Id}  Key: {metaData.Key}  Value: {metaData.Value}");

            Assert.IsTrue(metaData.Id == 1 || metaData.Id == 2);

            if (metaData.Id == 1)
            {
                Assert.AreEqual("Kittens", metaData.Key);
                Assert.AreEqual("Cute", metaData.Value);
            }

            if (metaData.Id == 2)
            {
                Assert.AreEqual("Puppies", metaData.Key);
                Assert.AreEqual("LotsOfWork", metaData.Value);
            }
        }

        // Verify direct add and retrieval.
        MetaData.AddOrUpdate(dataStore, "Puppies", "WorthIt!");
        MetaData.AddOrUpdate(dataStore, "Spiders", "Nope");
        Assert.AreEqual("Cute", MetaData.Get(dataStore, "Kittens"));
        Assert.AreEqual("WorthIt!", MetaData.Get(dataStore, "Puppies"));
        Assert.AreEqual("Nope", MetaData.Get(dataStore, "Spiders"));
        dataStoreMetaData = dataStore.Connection.GetAll<MetaData>().ToList();
        foreach (var metaData in dataStoreMetaData)
        {
            TestContext?.WriteLine($"  Id: {metaData.Id}  Key: {metaData.Key}  Value: {metaData.Value}");
        }
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void ReadAndWriteIdentity()
    {
        using var dataStore = new DataStore("TestStore", TestHelpers.GetDataStoreFilePath(TestOptions), TestOptions.DataStoreOptions.DataStoreSchema!);
        Assert.IsNotNull(dataStore);
        dataStore.Create();
        Assert.IsNotNull(dataStore.Connection);

        using var tx = dataStore.Connection.BeginTransaction();

        // Add Identity records
        dataStore.Connection.Insert(new Identity { Name = "Kitten1", InternalId = "11", Avatar = "https://www.microsoft.com" });
        dataStore.Connection.Insert(new Identity { Name = "Kitten2", InternalId = "12", Avatar = "https://www.microsoft.com" });
        tx.Commit();

        // Verify retrieval and input into data objects.
        var datastoreIdentities = dataStore.Connection.GetAll<Identity>().ToList();
        Assert.AreEqual(datastoreIdentities.Count, 2);
        for (var i = 1; i < 3; ++i)
        {
            var identity = Identity.Get(dataStore, i);
            Assert.IsNotNull(identity);
            Assert.AreEqual($"Kitten{i}", identity.Name);
            Assert.AreEqual($"1{i}", identity.InternalId);
            Assert.AreEqual("https://www.microsoft.com", identity.Avatar);

            var jsonString = identity.ToJson();
            TestContext?.WriteLine($"  Id: {identity.Id}  Name: {identity.Name}  AsJson: {jsonString}");
            var identity2 = Identity.FromJson(dataStore, jsonString);
            Assert.IsNotNull(identity2);
            Assert.AreEqual(identity.Name, identity2.Name);
        }
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void ReadAndWriteOrganization()
    {
        using var dataStore = new DataStore("TestStore", TestHelpers.GetDataStoreFilePath(TestOptions), TestOptions.DataStoreOptions.DataStoreSchema!);
        Assert.IsNotNull(dataStore);
        dataStore.Create();
        Assert.IsNotNull(dataStore.Connection);

        using var tx = dataStore.Connection.BeginTransaction();
        var org1 = Organization.GetOrCreate(dataStore, new Uri("https://dev.azure.com/organization1"));
        Assert.IsNotNull(org1);
        var org2 = Organization.GetOrCreate(dataStore, new Uri("https://organization2.visualstudio.com"));
        Assert.IsNotNull(org2);
        var org3 = Organization.GetOrCreate(dataStore, new Uri("https://dev.azure.com/organization3/"));
        Assert.IsNotNull(org3);
        tx.Commit();

        // Verify retrieval and input into data objects.
        var dataStoreOrganizations = dataStore.Connection.GetAll<Organization>().ToList();
        Assert.AreEqual(dataStoreOrganizations.Count, 3);
        for (var i = 1; i < 4; ++i)
        {
            var org = Organization.Get(dataStore, i);
            Assert.IsNotNull(org);
            Assert.AreEqual($"organization{i}", org.Name);
            Assert.AreEqual(org.Connection, org.ConnectionUri.ToString());
        }

        var orgLookup = Organization.Get(dataStore, "https://dev.azure.com/organization1");
        Assert.IsNotNull(orgLookup);
        Assert.AreEqual("organization1", orgLookup.Name);

        // Trailing slash on the org lookup by connection should give the same result.
        var orgLookup2 = Organization.Get(dataStore, "https://dev.azure.com/organization1/");
        Assert.IsNotNull(orgLookup2);
        Assert.AreEqual("organization1", orgLookup2.Name);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void ReadAndWriteProject()
    {
        using var dataStore = new DataStore("TestStore", TestHelpers.GetDataStoreFilePath(TestOptions), TestOptions.DataStoreOptions.DataStoreSchema!);
        Assert.IsNotNull(dataStore);
        dataStore.Create();
        Assert.IsNotNull(dataStore.Connection);

        using var tx = dataStore.Connection.BeginTransaction();
        var org = Organization.GetOrCreate(dataStore, new Uri("https://dev.azure.com/organization/"));
        Assert.IsNotNull(org);
        dataStore.Connection.Insert(new Project { Name = "P1", InternalId = "11", OrganizationId = org.Id });
        dataStore.Connection.Insert(new Project { Name = "P2", InternalId = "12", OrganizationId = org.Id });
        tx.Commit();

        // Verify retrieval and input into data objects.
        var dataStoreProjects = dataStore.Connection.GetAll<Project>().ToList();
        Assert.AreEqual(dataStoreProjects.Count, 2);
        for (var i = 1; i < 3; ++i)
        {
            var project = Project.Get(dataStore, i);
            Assert.IsNotNull(project);
            Assert.AreEqual($"P{i}", project.Name);
            Assert.AreEqual($"1{i}", project.InternalId);
            Assert.AreEqual("organization", project.Organization.Name);
            Assert.AreEqual("https://dev.azure.com/organization/", project.Organization.ConnectionUri.ToString());
            Assert.AreEqual($"https://dev.azure.com/organization/1{i}/", project.ConnectionUri.ToString());
        }

        var project2 = Project.Get(dataStore, "P2", "organization");
        Assert.IsNotNull(project2);
        Assert.AreEqual(2, project2.Id);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void ReadAndWriteRepository()
    {
        using var dataStore = new DataStore("TestStore", TestHelpers.GetDataStoreFilePath(TestOptions), TestOptions.DataStoreOptions.DataStoreSchema!);
        Assert.IsNotNull(dataStore);
        dataStore.Create();
        Assert.IsNotNull(dataStore.Connection);

        using var tx = dataStore.Connection.BeginTransaction();
        var org = Organization.GetOrCreate(dataStore, new Uri("https://dev.azure.com/organization/"));
        Assert.IsNotNull(org);
        dataStore.Connection.Insert(new Project { Name = "project", InternalId = "11", OrganizationId = org.Id });

        dataStore.Connection.Insert(new Repository { Name = "R1", InternalId = "21", CloneUrl = "https://organization/project/_git/repository1/", ProjectId = 1 });
        dataStore.Connection.Insert(new Repository { Name = "R2", InternalId = "22", CloneUrl = "https://organization/project/_git/repository2/", ProjectId = 1 });
        tx.Commit();

        // Verify retrieval and input into data objects.
        var dataStoreRepositories = dataStore.Connection.GetAll<Repository>().ToList();
        Assert.AreEqual(dataStoreRepositories.Count, 2);
        for (var i = 1; i < 3; ++i)
        {
            var repository = Repository.Get(dataStore, i);
            Assert.IsNotNull(repository);
            Assert.AreEqual($"R{i}", repository.Name);
            Assert.AreEqual($"2{i}", repository.InternalId);
            Assert.AreEqual("organization", repository.Project.Organization.Name);
            Assert.AreEqual("https://dev.azure.com/organization/", repository.Project.Organization.ConnectionUri.ToString());
            Assert.AreEqual($"https://organization/project/_git/repository{i}/", repository.Clone.Uri.ToString());
        }
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void ReadAndWriteQuery()
    {
        using var dataStore = new DataStore("TestStore", TestHelpers.GetDataStoreFilePath(TestOptions), TestOptions.DataStoreOptions.DataStoreSchema!);
        Assert.IsNotNull(dataStore);
        dataStore.Create();
        Assert.IsNotNull(dataStore.Connection);

        using var tx = dataStore.Connection.BeginTransaction();
        var org = Organization.GetOrCreate(dataStore, new Uri("https://dev.azure.com/organization/"));
        Assert.IsNotNull(org);
        dataStore.Connection.Insert(new Project { Name = "project", InternalId = "11", OrganizationId = org.Id });

        var q1 = Query.GetOrCreate(dataStore, "11", 1, "foo@bar", "Test Query");
        var q2 = Query.GetOrCreate(dataStore, "12", 1, "foo@bar", string.Empty);
        tx.Commit();

        // Verify retrieval and input into data objects.
        var dataStoreProjects = dataStore.Connection.GetAll<Query>().ToList();
        Assert.AreEqual(dataStoreProjects.Count, 2);
        for (var i = 1; i < 3; ++i)
        {
            var query = Query.Get(dataStore, i);
            Assert.IsNotNull(query);
            Assert.AreEqual($"foo@bar", query.Username);
            Assert.AreEqual($"1{i}", query.QueryId);
            Assert.AreEqual("organization", query.Project.Organization.Name);
        }

        var findQuery = Query.Get(dataStore, "12", "foo@bar");
        Assert.IsNotNull(findQuery);
        Assert.AreEqual("project", findQuery.Project.Name);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void ReadAndWriteWorkItemType()
    {
        using var dataStore = new DataStore("TestStore", TestHelpers.GetDataStoreFilePath(TestOptions), TestOptions.DataStoreOptions.DataStoreSchema!);
        Assert.IsNotNull(dataStore);
        dataStore.Create();
        Assert.IsNotNull(dataStore.Connection);

        using var tx = dataStore.Connection.BeginTransaction();
        var org = Organization.GetOrCreate(dataStore, new Uri("https://dev.azure.com/organization/"));
        Assert.IsNotNull(org);
        dataStore.Connection.Insert(new Project { Name = "project", InternalId = "11", OrganizationId = org.Id });

        dataStore.Connection.Insert(new WorkItemType { Name = "W1", Icon = "11", ProjectId = 1 });
        dataStore.Connection.Insert(new WorkItemType { Name = "W2", Icon = "12", ProjectId = 1 });
        tx.Commit();

        // Verify retrieval and input into data objects.
        var dataStoreProjects = dataStore.Connection.GetAll<WorkItemType>().ToList();
        Assert.AreEqual(dataStoreProjects.Count, 2);
        for (var i = 1; i < 3; ++i)
        {
            var workItemType = WorkItemType.Get(dataStore, i);
            Assert.IsNotNull(workItemType);
            Assert.AreEqual($"W{i}", workItemType.Name);
            Assert.AreEqual($"1{i}", workItemType.Icon);
            Assert.AreEqual("organization", workItemType.Project.Organization.Name);

            var jsonString = workItemType.ToJson();
            TestContext?.WriteLine($"  Name: {workItemType.Name}  AsJson: {jsonString}");
            var workItemType2 = WorkItemType.FromJson(dataStore, jsonString);
            Assert.IsNotNull(workItemType2);
            Assert.AreEqual(workItemType.Name, workItemType2.Name);
        }

        var findWorkItem = WorkItemType.Get(dataStore, "W2", 1);
        Assert.IsNotNull(findWorkItem);
        Assert.AreEqual("12", findWorkItem.Icon);
        Assert.AreEqual("project", findWorkItem.Project.Name);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void ReadAndWritePullRequestSearch()
    {
        using var dataStore = new DataStore("TestStore", TestHelpers.GetDataStoreFilePath(TestOptions), TestOptions.DataStoreOptions.DataStoreSchema!);
        Assert.IsNotNull(dataStore);
        dataStore.Create();
        Assert.IsNotNull(dataStore.Connection);

        using var tx = dataStore.Connection.BeginTransaction();
        var org = Organization.GetOrCreate(dataStore, new Uri("https://dev.azure.com/organization/"));
        Assert.IsNotNull(org);
        dataStore.Connection.Insert(new Project { Name = "project", InternalId = "11", OrganizationId = org.Id });
        dataStore.Connection.Insert(new Repository { Name = "repository1", InternalId = "21", CloneUrl = "https://organization/project/_git/repository1/", ProjectId = 1 });
        dataStore.Connection.Insert(new Repository { Name = "repository2", InternalId = "22", CloneUrl = "https://organization/project/_git/repository2/", ProjectId = 1 });

        var p1 = PullRequestSearch.GetOrCreate(dataStore, 1, 1, "foo@bar", PullRequestView.Mine);
        var p2 = PullRequestSearch.GetOrCreate(dataStore, 2, 1, "foo@bar", PullRequestView.Mine);
        tx.Commit();

        // Verify retrieval and input into data objects.
        var dataStoreProjects = dataStore.Connection.GetAll<PullRequestSearch>().ToList();
        Assert.AreEqual(dataStoreProjects.Count, 2);
        for (var i = 1; i < 3; ++i)
        {
            var pull = PullRequestSearch.Get(dataStore, i);
            Assert.IsNotNull(pull);
            Assert.AreEqual($"foo@bar", pull.Username);
            Assert.AreEqual($"repository{i}", pull.Repository.Name);
            Assert.AreEqual("organization", pull.Project.Organization.Name);
            Assert.AreEqual(PullRequestView.Mine, pull.View);
        }

        var findPull = PullRequestSearch.Get(dataStore, "organization", "project", "repository2", "foo@bar", PullRequestView.Mine);
        Assert.IsNotNull(findPull);
        Assert.AreEqual("repository2", findPull.Repository.Name);
        Assert.AreEqual(PullRequestView.Mine, findPull.View);
        Assert.AreEqual("project", findPull.Project.Name);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void ResetDataStore()
    {
        using var dataStore = new DataStore("TestStore", TestHelpers.GetDataStoreFilePath(TestOptions), TestOptions.DataStoreOptions.DataStoreSchema!);
        Assert.IsNotNull(dataStore);
        dataStore.Create();
        using var tx = dataStore.Connection!.BeginTransaction();

        var org = Organization.GetOrCreate(dataStore, new Uri("https://dev.azure.com/organization/"));
        Assert.IsNotNull(org);
        dataStore.Connection.Insert(new Project { Name = "project", InternalId = "11", OrganizationId = org.Id });
        dataStore.Connection.Insert(new Repository { Name = "repository1", InternalId = "21", CloneUrl = "https://organization/project/_git/repository1/", ProjectId = 1 });
        dataStore.Connection.Insert(new Repository { Name = "repository2", InternalId = "22", CloneUrl = "https://organization/project/_git/repository2/", ProjectId = 1 });

        tx.Commit();

        var dataStoreProjects = dataStore.Connection.GetAll<Repository>().ToList();
        Assert.AreEqual(2, dataStoreProjects.Count);

        dataStore.Reset();

        var dataStoreProjects2 = dataStore.Connection.GetAll<Repository>().ToList();
        Assert.AreEqual(0, dataStoreProjects2.Count);
    }
}
