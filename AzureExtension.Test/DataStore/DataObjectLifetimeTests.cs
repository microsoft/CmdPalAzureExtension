// Copyright (c) Microsoft Corporation
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
    public void DeleteBeforeBuilds()
    {
        using var dataStore = new DataStore("TestStore", TestHelpers.GetDataStoreFilePath(TestOptions), TestOptions.DataStoreOptions.DataStoreSchema!);
        dataStore.Create();

        using var tx = dataStore.Connection!.BeginTransaction();
        var now = DateTime.UtcNow;

        dataStore.Connection.Insert(new Project { Id = 1, Name = "TestProject" });
        dataStore.Connection.Insert(new Definition { Id = 1, Name = "TestDefinition", ProjectId = 1 });

        var builds = new List<Build>
        {
            new() { Id = 1, DefinitionId = 1, TimeUpdated = now.ToDataStoreInteger() - 200 },
            new() { Id = 2, DefinitionId = 1, TimeUpdated = now.ToDataStoreInteger() - 100 },
        };

        for (var i = 0; i < builds.Count; i++)
        {
            builds[i].Id = dataStore.Connection.Insert(builds[i]);
        }

        var dsDefinition = dataStore.Connection.Get<Definition>(1);
        Build.DeleteBefore(dataStore, now - TimeSpan.FromTicks(150));

        tx.Commit();

        var dataStoreBuilds = dataStore.Connection.GetAll<Build>().ToList();
        Assert.AreEqual(1, dataStoreBuilds.Count);
        Assert.AreEqual(2, dataStoreBuilds[0].Id);
        Assert.AreEqual(dsDefinition.Id, dataStoreBuilds[0].DefinitionId);
    }

    [TestMethod]
    public void DeleteUnreferencedDefinition()
    {
        using var dataStore = new DataStore("TestStore", TestHelpers.GetDataStoreFilePath(TestOptions), TestOptions.DataStoreOptions.DataStoreSchema!);
        dataStore.Create();

        using var tx = dataStore.Connection!.BeginTransaction();

        dataStore.Connection.Insert(new Project { Id = 1, Name = "TestProject" });
        dataStore.Connection.Insert(new Definition { Id = 1, Name = "TestDefinition1", ProjectId = 1 });
        dataStore.Connection.Insert(new Definition { Id = 2, Name = "TestDefinition2", ProjectId = 1 });
        var builds = new List<Build>
        {
            new() { Id = 1, DefinitionId = 2, },
            new() { Id = 2, DefinitionId = 2, },
        };

        for (var i = 0; i < builds.Count; i++)
        {
            builds[i].Id = dataStore.Connection.Insert(builds[i]);
        }

        var dsDefinition = dataStore.Connection.Get<Definition>(1);

        Definition.DeleteUnreferenced(dataStore);

        tx.Commit();

        var dataStoreDefinitions = dataStore.Connection.GetAll<Definition>().ToList();

        Assert.AreEqual(1, dataStoreDefinitions.Count);
        Assert.AreEqual(2, dataStoreDefinitions[0].Id);
        Assert.AreEqual("TestDefinition2", dataStoreDefinitions[0].Name);
    }

    [TestMethod]
    public void UpdatingFreshDefinition()
    {
        using var dataStore = new DataStore("TestStore", TestHelpers.GetDataStoreFilePath(TestOptions), TestOptions.DataStoreOptions.DataStoreSchema!);
        dataStore.Create();

        using var tx = dataStore.Connection!.BeginTransaction();

        var now = DateTime.UtcNow;
        dataStore.Connection.Insert(new Project { Id = 1, Name = "TestProject" });

        var definition = new Definition { InternalId = 1, Name = "TestDefinition1", ProjectId = 1, TimeUpdated = now.ToDataStoreInteger() };
        dataStore.Connection.Insert(definition);

        var secondDefinition = new Definition
        {
            InternalId = 1,
            Name = "TestDefinition2",
            ProjectId = 1,
            TimeUpdated = now.ToDataStoreInteger() + 100,
        };

        Definition.AddOrUpdate(dataStore, secondDefinition);

        tx.Commit();

        var dsDefinition = dataStore.Connection.Get<Definition>(1);
        Assert.AreEqual("TestDefinition1", dsDefinition.Name);
    }

    [TestMethod]
    public void UpdatingOldDefinition()
    {
        using var dataStore = new DataStore("TestStore", TestHelpers.GetDataStoreFilePath(TestOptions), TestOptions.DataStoreOptions.DataStoreSchema!);
        dataStore.Create();

        using var tx = dataStore.Connection!.BeginTransaction();

        var now = DateTime.UtcNow;
        dataStore.Connection.Insert(new Project { Id = 1, Name = "TestProject" });

        var definition = new Definition { InternalId = 1, Name = "TestDefinition1", ProjectId = 1, TimeUpdated = now.ToDataStoreInteger() };
        dataStore.Connection.Insert(definition);

        var secondDefinition = new Definition
        {
            InternalId = 1,
            Name = "TestDefinition2",
            ProjectId = 1,
            TimeUpdated = now.ToDataStoreInteger() + TimeSpan.FromHours(5).ToDataStoreInteger(), // Update threshold is 4 hours
        };

        Definition.AddOrUpdate(dataStore, secondDefinition);

        tx.Commit();

        var dsDefinition = dataStore.Connection.Get<Definition>(1);
        Assert.AreEqual("TestDefinition2", dsDefinition.Name);
    }
}
