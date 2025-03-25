// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Microsoft.CommandPalette.Extensions;

namespace AzureExtension;

[ComVisible(true)]
[Guid("23c9363b-1ade-4017-afa7-f57f0351bca1")]
[ComDefaultInterface(typeof(IExtension))]
public sealed partial class AzureExtension : IExtension, IDisposable
{
    private readonly ManualResetEvent _extensionDisposedEvent;

    private readonly AzureExtensionActionsProvider _provider = new();

    public AzureExtension(ManualResetEvent extensionDisposedEvent)
    {
        this._extensionDisposedEvent = extensionDisposedEvent;
    }

    public object GetProvider(ProviderType providerType)
    {
        switch (providerType)
        {
            case ProviderType.Commands:
                return _provider;
            default:
#pragma warning disable CS8603 // Possible null reference return.
                return null;
#pragma warning restore CS8603 // Possible null reference return.
        }
    }

    public void Dispose()
    {
        this._extensionDisposedEvent.Set();
    }
}
