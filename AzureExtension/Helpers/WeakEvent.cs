// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace AzureExtension.Helpers;

#pragma warning disable SA1649 // File name should match first type name
public class WeakEvent<TEventArgs>
    where TEventArgs : EventArgs
{
    private readonly List<WeakReference<IWeakListener<TEventArgs>>> _handlers = new();

    public void AddListener(IWeakListener<TEventArgs> handler)
    {
        lock (_handlers)
        {
            _handlers.Add(new WeakReference<IWeakListener<TEventArgs>>(handler));
        }
    }

    public void RemoveListener(IWeakListener<TEventArgs> handler)
    {
        lock (_handlers)
        {
            _handlers.RemoveAll(h =>
            {
                if (h.TryGetTarget(out var target))
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
            _handlers.RemoveAll(h =>
            {
                if (h.TryGetTarget(out var target))
                {
                    target.OnEvent(sender, args);
                    return false;
                }

                return true;
            });
        }
    }
}

public interface IWeakListener<TEventArgs>
    where TEventArgs : EventArgs
{
    void OnEvent(object? sender, TEventArgs args);
}
