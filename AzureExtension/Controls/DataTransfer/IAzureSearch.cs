// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace AzureExtension.Controls;

public interface IAzureSearch
{
    string Name { get; }

    string Url { get; }

    bool IsTopLevel { get; }
}
