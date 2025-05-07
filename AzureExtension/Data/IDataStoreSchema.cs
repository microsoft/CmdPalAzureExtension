// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace AzureExtension.Data;

public interface IDataStoreSchema
{
    long SchemaVersion
    {
        get;
    }

    List<string> SchemaSqls
    {
        get;
    }
}
