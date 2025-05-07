// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Data;
using AzureExtension.DataModel;
using Dapper.Contrib.Extensions;

namespace AzureExtension.Test;

public partial class DataStoreTests
{
    [TestMethod]
    public void GetStatusForDefinition()
    {
        using var dataStore = new DataStore("TestStore", TestHelpers.GetDataStoreFilePath(TestOptions), TestOptions.DataStoreOptions.DataStoreSchema!);
        dataStore.Create();

        using var tx = dataStore.Connection!.BeginTransaction();

        dataStore.Connection.Insert(new Project { Id = 1, Name = "TestProject" });
        dataStore.Connection.Insert(new Definition { Id = 1, Name = "TestDefinition", ProjectId = 1, InternalId = 1 });

        var builds = new List<Build>
        {
            new() { Id = 1, DefinitionId = 1, Status = "Failed", TimeUpdated = 1, },
            new() { Id = 2, DefinitionId = 1, Status = "In progress", TimeUpdated = 2 },
            new() { Id = 3, DefinitionId = 1, Status = "Success", TimeUpdated = 3 },
        };

        for (var i = 0; i < builds.Count; i++)
        {
            builds[i].Id = dataStore.Connection.Insert(builds[i]);
        }

        tx.Commit();

        var dsDefinition = Definition.GetByInternalId(dataStore, 1);

        var status = dsDefinition!.Status;

        Assert.AreEqual("Failed", status);
    }

    [TestMethod]
    public void StatusForDefinitionNoBuilds()
    {
        using var dataStore = new DataStore("TestStore", TestHelpers.GetDataStoreFilePath(TestOptions), TestOptions.DataStoreOptions.DataStoreSchema!);
        dataStore.Create();

        using var tx = dataStore.Connection!.BeginTransaction();

        dataStore.Connection.Insert(new Project { Id = 1, Name = "TestProject" });
        dataStore.Connection.Insert(new Definition { Id = 1, Name = "TestDefinition", ProjectId = 1, InternalId = 1 });

        tx.Commit();

        var dsDefinition = Definition.GetByInternalId(dataStore, 1);
        var status = dsDefinition!.Status;

        Assert.AreEqual(string.Empty, status);
    }
}
