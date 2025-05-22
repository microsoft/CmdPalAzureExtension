// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace AzureExtension.Helpers;

#pragma warning disable SA1649 // File name should match first type name
public class WeakEvent<TEventArgs>
    where TEventArgs : EventArgs
{
    private sealed class WeakHandlerEntry
    {
        private readonly WeakReference<object>? _targetReference;
        private readonly EventHandler<TEventArgs> _originalHandler;
        private readonly bool _isStatic;

        public WeakHandlerEntry(EventHandler<TEventArgs> handler)
        {
            _originalHandler = handler;
            _isStatic = handler.Target == null;

            // Only create a weak reference if the target exists (non-static method)
            if (!_isStatic && handler.Target != null)
            {
                _targetReference = new WeakReference<object>(handler.Target);
            }
        }

        public bool Invoke(object? sender, TEventArgs args)
        {
            // For static methods, we can always invoke
            if (_isStatic)
            {
                _originalHandler(sender, args);
                return true;
            }

            // For instance methods, check if the target is still alive
            if (_targetReference != null && _targetReference.TryGetTarget(out _))
            {
                _originalHandler(sender, args);
                return true;
            }

            // Target was collected
            return false;
        }

        public bool Matches(EventHandler<TEventArgs> handler)
        {
            return _originalHandler == handler;
        }
    }

    private readonly List<WeakHandlerEntry> _handlers = new();

    public void AddListener(EventHandler<TEventArgs> handler)
    {
        lock (_handlers)
        {
            _handlers.Add(new WeakHandlerEntry(handler));
        }
    }

    public void RemoveListener(EventHandler<TEventArgs> handler)
    {
        lock (_handlers)
        {
            _handlers.RemoveAll(h => h.Matches(handler));
        }
    }

    public void Raise(object? sender, TEventArgs args)
    {
        lock (_handlers)
        {
            _handlers.RemoveAll(handler => !handler.Invoke(sender, args));
        }
    }
}
