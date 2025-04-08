// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Client;
using AzureExtension.Controls;
using AzureExtension.DeveloperId;

namespace AzureExtension.PersistentData;

public interface IAzureValidator
{
    public abstract InfoResult GetQueryInfo(string queryUrl, string queryName, IDeveloperId developerId);
}
