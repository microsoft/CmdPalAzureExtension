// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace AzureExtension.Client;

public class VssConnectionFactory : IVssConnectionFactory
{
    public IVssConnection CreateVssConnection(Uri uri, VssCredentials credentials)
    {
        return new VssConnection(uri, credentials);
    }
}

public interface IVssConnectionFactory
{
    IVssConnection CreateVssConnection(Uri uri, VssCredentials credentials);
}
