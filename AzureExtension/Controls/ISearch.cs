// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.DataModel;
using AzureExtension.DeveloperId;

namespace AzureExtension.Controls;

public interface ISearch
{
    // The display name that a user enters, which could be the query name in ADO
    string Name { get; }

    string SearchString { get; }
}
