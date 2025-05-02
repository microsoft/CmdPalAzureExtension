// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Data;

namespace AzureExtension.PersistentData;

public sealed class PersistentDataSchema : IDataStoreSchema
{
    public long SchemaVersion => 2;

    public List<string> SchemaSqls => _schemaSqlsValue;

    private const string Query =
        @"CREATE TABLE IF NOT EXISTS Query (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL,
            Url TEXT NOT NULL,
            IsTopLevel INTEGER NOT NULL CHECK (IsTopLevel IN (0, 1))
        )";

    private const string PullRequestSearch =
        @"CREATE TABLE IF NOT EXISTS PullRequestSearch (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Url TEXT NOT NULL,
            Name TEXT NOT NULL,
            View TEXT NOT NULL,
            IsTopLevel INTEGER NOT NULL CHECK (IsTopLevel IN (0, 1))
        )";

    private const string DefinitionSearch =
        @"CREATE TABLE IF NOT EXISTS DefinitionSearch (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            InternalId INTEGER NOT NULL,
            ProjectUrl TEXT NOT NULL,
            IsTopLevel INTEGER NOT NULL CHECK (IsTopLevel IN (0, 1))
        )";

    private static readonly List<string> _schemaSqlsValue = new()
    {
        Query,
        PullRequestSearch,
        DefinitionSearch,
    };
}
