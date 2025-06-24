// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace AzureExtension;

public class DataStoreException : Exception
{
    public DataStoreException()
    {
    }

    public DataStoreException(string message)
        : base(message)
    {
    }
}
