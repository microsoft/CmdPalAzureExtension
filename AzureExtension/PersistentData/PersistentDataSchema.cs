// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.DataModel;

namespace AzureExtension.PersistentData;

public sealed class PersistentDataSchema : IDataStoreSchema
{
    public long SchemaVersion => 1;

    public List<string> SchemaSqls => _schemaSqlsValue;

    private const string Query =
        @"CREATE TABLE IF NOT EXISTS Search (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL,
            Url TEXT NOT NULL,
            IsTopLevel INTEGER NOT NULL CHECK (IsTopLevel IN (0, 1))
        )";

    private static readonly List<string> _schemaSqlsValue = new()
    {
        Query,
    };
}
