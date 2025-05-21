// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace AzureExtension.Helpers;

#pragma warning disable SA1649 // File name should match first type name
public class WeakEvent<TEventArgs>
    where TEventArgs : EventArgs
{
    private readonly List<WeakReference<EventHandler<TEventArgs>>> _handlers = new();

    public void AddListener(EventHandler<TEventArgs> handler)
    {
        lock (_handlers)
        {
            _handlers.Add(new WeakReference<EventHandler<TEventArgs>>(handler));
        }
    }

    public void RemoveListener(EventHandler<TEventArgs> handler)
    {
        lock (_handlers)
        {
            _handlers.RemoveAll(wr =>
            {
                if (wr.TryGetTarget(out var target))
                {
                    return target == handler;
                }

                return true;
            });
        }
    }

    public void Raise(object? sender, TEventArgs args)
    {
        lock (_handlers)
        {
            _handlers.RemoveAll(wr =>
            {
                if (wr.TryGetTarget(out var target))
                {
                    target.Invoke(sender, args);
                    return false;
                }

                return true;
            });
        }
    }
}
