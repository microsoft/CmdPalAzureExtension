// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Data;

namespace AzureExtension.DataModel;

public class AzureCacheDataStoreSchema : IDataStoreSchema
{
    public long SchemaVersion => SchemaVersionValue;

    public List<string> SchemaSqls => _schemaSqlsValue;

    public AzureCacheDataStoreSchema()
    {
    }

    // Update this anytime incompatible changes happen with a released version.
    private const long SchemaVersionValue = 0x0010;

    private const string Metadata =
    @"CREATE TABLE Metadata (" +
        "Id INTEGER PRIMARY KEY NOT NULL," +
        "Key TEXT NOT NULL COLLATE NOCASE," +
        "Value TEXT NOT NULL" +
    ");" +
    "CREATE UNIQUE INDEX IDX_Metadata_Key ON Metadata (Key);";

    private const string Identity =
    @"CREATE TABLE Identity (" +
        "Id INTEGER PRIMARY KEY NOT NULL," +
        "Name TEXT NOT NULL COLLATE NOCASE," +
        "InternalId TEXT NOT NULL," +
        "Avatar TEXT NOT NULL COLLATE NOCASE," +
        "DeveloperLoginId TEXT," +
        "TimeUpdated INTEGER NOT NULL" +
    ");" +

    // InternalId is a Guid from the server, so by definition is unique.
    "CREATE UNIQUE INDEX IDX_Identity_InternalId ON Identity (InternalId);";

    private const string Project =
    @"CREATE TABLE Project (" +
        "Id INTEGER PRIMARY KEY NOT NULL," +
        "Name TEXT NOT NULL COLLATE NOCASE," +
        "InternalId TEXT NOT NULL," +
        "Description TEXT NOT NULL," +
        "OrganizationId INTEGER NOT NULL," +
        "TimeUpdated INTEGER NOT NULL" +
    ");" +

    // Project ID is a Guid, so by definition is unique.
    // Project Name can be renamed and reused per DevOps documentation, so it is not safe.
    "CREATE UNIQUE INDEX IDX_Project_InternalId ON Project (InternalId);";

    private const string ProjectReference =
    @"CREATE TABLE ProjectReference (" +
        "Id INTEGER PRIMARY KEY NOT NULL," +
        "ProjectId INTEGER NOT NULL," +
        "DeveloperId INTEGER NOT NULL," +
        "PullRequestCount INTEGER NOT NULL" +
    ");" +

    // Project references are unique by DeveloperId and ProjectId
    "CREATE UNIQUE INDEX IDX_ProjectReference_ProjectIdDeveloperId ON ProjectReference (ProjectId, DeveloperId);";

    private const string Organization =
    @"CREATE TABLE Organization (" +
        "Id INTEGER PRIMARY KEY NOT NULL," +
        "Name TEXT NOT NULL COLLATE NOCASE," +
        "Connection TEXT NOT NULL COLLATE NOCASE," +
        "TimeUpdated INTEGER NOT NULL," +
        "TimeLastSync INTEGER NOT NULL" +
    ");" +

    // Connections should be unique per organization. We do not appear to have
    // better information about the organization and connection at this time, so
    // we will use Connection as the unique constraint. It is also used in the
    // constructor, so while the same organization may have multiple connections,
    // each connection should correspond to only one organization.
    "CREATE UNIQUE INDEX IDX_Organization_Connection ON Organization (Connection);";

    private const string Repository =
    @"CREATE TABLE Repository (" +
        "Id INTEGER PRIMARY KEY NOT NULL," +
        "Name TEXT NOT NULL COLLATE NOCASE," +
        "InternalId TEXT NOT NULL," +
        "ProjectId INTEGER NOT NULL," +
        "CloneUrl TEXT NOT NULL COLLATE NOCASE," +
        "IsPrivate INTEGER NOT NULL," +
        "TimeUpdated INTEGER NOT NULL" +
    ");" +

    // While Name and ProjectId should be unique, it is possible renaming occurs and
    // we might have a collision if we have cached a repository prior to rename and
    // then encounter a different repository with that name. Therefore we will not
    // create a unique index on ProjectId and Name.
    // Repository InternalId is a Guid, so by definition is unique.
    "CREATE UNIQUE INDEX IDX_Repository_InternalId ON Repository (InternalId);";

    private const string RepositoryReference =
    @"CREATE TABLE RepositoryReference (" +
        "Id INTEGER PRIMARY KEY NOT NULL," +
        "RepositoryId INTEGER NOT NULL," +
        "DeveloperId INTEGER NOT NULL," +
        "PullRequestCount INTEGER NOT NULL" +
    ");" +

    // Repository references are unique by DeveloperId and ProjectId
    "CREATE UNIQUE INDEX IDX_RepositoryReference_RepositoryIdDeveloperId ON RepositoryReference (RepositoryId, DeveloperId);";

    private const string Query =
    @"CREATE TABLE Query (" +
        "Id INTEGER PRIMARY KEY NOT NULL," +
        "QueryId TEXT NOT NULL COLLATE NOCASE," +
        "DisplayName TEXT NOT NULL," +
        "Username TEXT NOT NULL COLLATE NOCASE," +
        "ProjectId INTEGER NOT NULL," +
        "TimeUpdated INTEGER NOT NULL" +
    ");" +

    // Queries can differ in their results by developerId, so while QueryId is
    // a guid, the result information could depend on which developer is making
    // the query. Therefore we make unique constraint on QueryId AND DeveloperLogin.
    "CREATE UNIQUE INDEX IDX_Query_QueryIdUsername ON Query (QueryId, Username);";

    private const string WorkItem =
    @"CREATE TABLE WorkItem (" +
        "Id INTEGER PRIMARY KEY NOT NULL," +
        "InternalId INTEGER NOT NULL," +
        "SystemTitle TEXT NOT NULL COLLATE NOCASE," +
        "HtmlUrl TEXT NOT NULL COLLATE NOCASE," +
        "SystemState TEXT NOT NULL COLLATE NOCASE," +
        "SystemReason TEXT NOT NULL COLLATE NOCASE," +
        "SystemAssignedToId INTEGER NOT NULL," +
        "SystemCreatedDate INTEGER NOT NULL," +
        "SystemCreatedById INTEGER NOT NULL," +
        "SystemChangedDate INTEGER NOT NULL," +
        "SystemChangedById INTEGER NOT NULL," +
        "SystemWorkItemTypeId INTEGER NOT NULL" +
    ");";

    private const string QueryWorkItem =
    @"CREATE TABLE QueryWorkItem (" +
        "Id INTEGER PRIMARY KEY NOT NULL," +
        "Query INTEGER NOT NULL," +
        "WorkItem INTEGER NOT NULL," +
        "TimeUpdated INTEGER NOT NULL" +
    ");" +

    // QueryWorkItem is unique by QueryId and WorkItemId
    "CREATE UNIQUE INDEX IDX_QueryWorkItem_QueryWorkItem ON QueryWorkItem (Query, WorkItem);";

    private const string WorkItemType =
    @"CREATE TABLE WorkItemType (" +
        "Id INTEGER PRIMARY KEY NOT NULL," +
        "Name TEXT NOT NULL COLLATE NOCASE," +
        "Icon TEXT NOT NULL," +
        "Color TEXT NOT NULL," +
        "Description TEXT NOT NULL," +
        "ProjectId INTEGER NOT NULL," +
        "TimeUpdated INTEGER NOT NULL" +
    ");" +

    // WorkItemType is looked up on the server by Name and Project name/guid, so a unique
    // constraint on Name and ProjectId is consistent and safe.
    "CREATE UNIQUE INDEX IDX_WorkItemType_NameProjectId ON WorkItemType (Name, ProjectId);";

    private const string PullRequestSearch =
    @"CREATE TABLE PullRequestSearch (" +
        "Id INTEGER PRIMARY KEY NOT NULL," +
        "RepositoryId INTEGER NOT NULL," +
        "Username TEXT NOT NULL COLLATE NOCASE," +
        "ProjectId INTEGER NOT NULL," +
        "ViewId INTEGER NOT NULL," +
        "TimeUpdated INTEGER NOT NULL" +
    ");" +

    // Developer Pull requests are unique on Org / Project / Repository and
    // the developer login, and the view.
    "CREATE UNIQUE INDEX IDX_PullRequestSearch_ProjectIdRepositoryIdDeveloperLoginViewId ON PullRequestSearch (ProjectId, RepositoryId, Username, ViewId);";

    private const string PullRequest =
    @"CREATE TABLE PullRequest (" +
        "Id INTEGER PRIMARY KEY NOT NULL," +
        "InternalId INTEGER NOT NULL," +
        "Title TEXT NOT NULL COLLATE NOCASE," +
        "Url TEXT NOT NULL COLLATE NOCASE," +
        "RepositoryId INTEGER NOT NULL," +
        "CreatorId INTEGER NOT NULL," +
        "Status TEXT NOT NULL COLLATE NOCASE," +
        "PolicyStatus TEXT NOT NULL COLLATE NOCASE," +
        "PolicyStatusReason TEXT NOT NULL COLLATE NOCASE," +
        "TargetBranch TEXT NOT NULL COLLATE NOCASE," +
        "CreationDate INTEGER NOT NULL," +
        "HtmlUrl TEXT NOT NULL COLLATE NOCASE" +
    ");";

    private const string PullRequestSearchPullRequest =
    @"CREATE TABLE PullRequestSearchPullRequest (" +
        "Id INTEGER PRIMARY KEY NOT NULL," +
        "PullRequestSearch INTEGER NOT NULL," +
        "PullRequest INTEGER NOT NULL," +
        "TimeUpdated INTEGER NOT NULL" +
    ");" +

    // PullRequestSearchPullRequest is unique by PullRequestSearch and PullRequest
    "CREATE UNIQUE INDEX IDX_PullRequestSearchPullRequest_PullRequestSearchPullRequest ON PullRequestSearchPullRequest (PullRequestSearch, PullRequest);";

    // PullRequsetPolicyStatus is a snapshot of a developer's Pull Requests.
    private const string PullRequestPolicyStatus =
    @"CREATE TABLE PullRequestPolicyStatus (" +
        "Id INTEGER PRIMARY KEY NOT NULL," +
        "ArtifactId TEXT NULL COLLATE NOCASE," +
        "ProjectId INTEGER NOT NULL," +
        "RepositoryId INTEGER NOT NULL," +
        "PullRequestId INTEGER NOT NULL," +
        "Title TEXT NULL COLLATE NOCASE," +
        "PolicyStatusId INTEGER NOT NULL," +
        "PolicyStatusReason TEXT NULL COLLATE NOCASE," +
        "PullRequestStatusId INTEGER NOT NULL," +
        "TargetBranchName TEXT NULL COLLATE NOCASE," +
        "HtmlUrl TEXT NULL COLLATE NOCASE," +
        "TimeUpdated INTEGER NOT NULL," +
        "TimeCreated INTEGER NOT NULL" +
    ");";

    private const string Definition =
    @"CREATE TABLE Definition (" +
        "Id INTEGER PRIMARY KEY NOT NULL," +
        "InternalId INTEGER NOT NULL," +
        "Name TEXT NOT NULL COLLATE NOCASE," +
        "ProjectId INTEGER NOT NULL," +
        "CreationDate INTEGER NOT NULL," +
        "HtmlUrl TEXT NOT NULL COLLATE NOCASE," +
        "TimeUpdated INTEGER NOT NULL" +
    ");";

    private const string Build =
    @"CREATE TABLE Build (" +
        "Id INTEGER PRIMARY KEY NOT NULL," +
        "InternalId INTEGER NOT NULL," +
        "BuildNumber TEXT NOT NULL COLLATE NOCASE," +
        "Status TEXT NOT NULL COLLATE NOCASE," +
        "Result TEXT NOT NULL COLLATE NOCASE," +
        "QueueTime INTEGER NOT NULL," +
        "StartTime INTEGER NOT NULL," +
        "FinishTime INTEGER NOT NULL," +
        "Url TEXT NOT NULL COLLATE NOCASE," +
        "DefinitionId INTEGER NOT NULL," +
        "SourceBranch TEXT NOT NULL COLLATE NOCASE," +
        "TriggerMessage TEXT NOT NULL COLLATE NOCASE," +
        "RequesterId INTEGER NOT NULL," +
        "TimeUpdated INTEGER NOT NULL" +
    ");";

    private const string Notification =
    @"CREATE TABLE Notification (" +
        "Id INTEGER PRIMARY KEY NOT NULL," +
        "TypeId INTEGER NOT NULL," +
        "ProjectId INTEGER NOT NULL," +
        "RepositoryId INTEGER NOT NULL," +
        "Title TEXT NOT NULL COLLATE NOCASE," +
        "Description TEXT NOT NULL COLLATE NOCASE," +
        "Identifier TEXT NULL COLLATE NOCASE," +
        "Result TEXT NULL COLLATE NOCASE," +
        "HtmlUrl TEXT NULL COLLATE NOCASE," +
        "ToastState INTEGER NOT NULL," +
        "TimeCreated INTEGER NOT NULL" +
    ");";

    // All Sqls together.
    private static readonly List<string> _schemaSqlsValue =
    [
        Metadata,
        Identity,
        Project,
        ProjectReference,
        Organization,
        Repository,
        RepositoryReference,
        Query,
        WorkItem,
        QueryWorkItem,
        WorkItemType,
        PullRequestSearch,
        PullRequest,
        PullRequestSearchPullRequest,
        PullRequestPolicyStatus,
        Definition,
        Build,
        Notification,
    ];
}
