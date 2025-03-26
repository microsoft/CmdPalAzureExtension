// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Services.Common;

namespace AzureExtension.DeveloperId;

public interface IDeveloperId
{
    string LoginId { get; }

    string Url { get; }

    VssCredentials? GetCredentials();
}
