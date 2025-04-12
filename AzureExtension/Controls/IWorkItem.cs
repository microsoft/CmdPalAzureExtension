// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace AzureExtension.Controls;

public interface IWorkItem
{
    string SystemTitle { get; }

    long Id { get; }

    string HtmlUrl { get; }
}
