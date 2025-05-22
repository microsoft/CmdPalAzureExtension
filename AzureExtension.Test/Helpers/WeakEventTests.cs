// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using AzureExtension.Helpers;
using Microsoft.VisualStudio.Services.Common;
using Moq;

namespace AzureExtension.Test.Helpers;

[TestClass]
public class WeakEventTests
{
    public sealed class TestEventArgs : EventArgs
    {
        public int Value { get; set; }
    }

    private sealed class EventListener : IWeakListener<TestEventArgs>
    {
        public int CallCount { get; set; }

        public int LastValue { get; private set; }

        public void OnEvent(object? sender, TestEventArgs args)
        {
            CallCount++;
            LastValue = args.Value;
        }
    }

    private sealed class AnonymousEventListener : IWeakListener<TestEventArgs>
    {
        private readonly Action<object?, TestEventArgs> _handler;

        public AnonymousEventListener(Action<object?, TestEventArgs> handler)
        {
            _handler = handler;
        }

        public void OnEvent(object? sender, TestEventArgs args)
        {
            _handler(sender, args);
        }
    }

    private sealed class StaticEventListener : IWeakListener<TestEventArgs>
    {
        public void OnEvent(object? sender, TestEventArgs args)
        {
            StaticHandlerCounter.Count++;
        }
    }

    [TestMethod]
    public void AddListener_ShouldRegisterHandler()
    {
        // Arrange
        var weakEvent = new WeakEvent<TestEventArgs>();
        int invocationCount = 0;
        var listener = new AnonymousEventListener((sender, e) => invocationCount++);
        var args = new TestEventArgs { Value = 42 };

        // Act
        weakEvent.AddListener(listener);
        weakEvent.Raise(this, args);

        // Assert
        Assert.AreEqual(1, invocationCount, "Handler should be called once");
    }

    [TestMethod]
    public void RemoveListener_ShouldUnregisterHandler()
    {
        // Arrange
        var weakEvent = new WeakEvent<TestEventArgs>();
        var listener = new EventListener();

        // Act
        weakEvent.AddListener(listener);
        weakEvent.RemoveListener(listener);
        weakEvent.Raise(this, new TestEventArgs { Value = 42 });

        // Assert
        Assert.AreEqual(0, listener.CallCount, "Handler should not be called after removal");
    }

    [TestMethod]
    public void Raise_ShouldInvokeAllHandlers()
    {
        // Arrange
        var weakEvent = new WeakEvent<TestEventArgs>();
        var listener1 = new EventListener();
        var listener2 = new EventListener();
        var args = new TestEventArgs { Value = 42 };

        // Act
        weakEvent.AddListener(listener1);
        weakEvent.AddListener(listener2);
        weakEvent.Raise(this, args);

        // Assert
        Assert.AreEqual(1, listener1.CallCount, "First listener should be called once");
        Assert.AreEqual(1, listener2.CallCount, "Second listener should be called once");
        Assert.AreEqual(42, listener1.LastValue, "First listener should receive correct value");
        Assert.AreEqual(42, listener2.LastValue, "Second listener should receive correct value");
    }

    [TestMethod]
    public void WeakEvent_ShouldAddListener()
    {
        // Test that listeners are added correctly
        var weakEvent = new WeakEvent<TestEventArgs>();
        var listener = new EventListener();

        weakEvent.AddListener(listener);

        var fieldInfo = typeof(WeakEvent<TestEventArgs>).GetField("_handlers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var handlers = (List<WeakReference<IWeakListener<TestEventArgs>>>)fieldInfo!.GetValue(weakEvent)!;

        Assert.AreEqual(1, handlers.Count, "Handler should be added");

        bool targetResolved = handlers[0].TryGetTarget(out var target);
        Assert.IsTrue(targetResolved, "Should resolve target");
        Assert.AreSame(listener, target, "Should resolve to the correct listener");
    }

    [TestMethod]
    public void WeakEvent_ShouldRemoveDeadReferences()
    {
        // Test specifically the cleanup logic
        var weakEvent = new WeakEvent<TestEventArgs>();

        // Use reflection to directly access the handlers list
        var fieldInfo = typeof(WeakEvent<TestEventArgs>).GetField("_handlers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var handlers = (List<WeakReference<IWeakListener<TestEventArgs>>>)fieldInfo!.GetValue(weakEvent)!;

        // Add a known dead weak reference
        handlers.Add(new WeakReference<IWeakListener<TestEventArgs>>(null!));

        // Trigger cleanup by raising the event
        weakEvent.Raise(this, new TestEventArgs());

        // Verify the dead reference was removed
        Assert.AreEqual(0, handlers.Count, "Dead reference should be removed");
    }

    [TestMethod]
    public void WeakEvent_ShouldInvokeRemainingHandlersAfterGarbageCollection()
    {
        // Arrange
        var weakEvent = new WeakEvent<TestEventArgs>();
        var permanentListener = new EventListener();

        // Act - add a permanent listener and a temporary one
        weakEvent.AddListener(permanentListener);
        {
            var temporaryListener = new EventListener();
            weakEvent.AddListener(temporaryListener);

            // Verify both work initially
            weakEvent.Raise(this, new TestEventArgs { Value = 42 });
            Assert.AreEqual(1, permanentListener.CallCount, "Permanent listener should be called");
            Assert.AreEqual(1, temporaryListener.CallCount, "Temporary listener should be called");

            // Clear reference to temporary listener
            temporaryListener = null;
        }

        // Force garbage collection
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);

        // Reset counter
        permanentListener.CallCount = 0;

        // Raise event again
        weakEvent.Raise(this, new TestEventArgs { Value = 100 });

        // Assert
        Assert.AreEqual(1, permanentListener.CallCount, "Permanent listener should still be called after GC");
        Assert.AreEqual(100, permanentListener.LastValue, "Permanent listener should receive correct value");
    }

    [TestMethod]
    public void WeakEvent_ShouldRemoveHandlersWhenTargetCannotBeResolved()
    {
        // Arrange
        var weakEvent = new WeakEvent<TestEventArgs>();

        // Get access to the handlers list through reflection
        var fieldInfo = typeof(WeakEvent<TestEventArgs>).GetField("_handlers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var handlers = (List<WeakReference<IWeakListener<TestEventArgs>>>)fieldInfo!.GetValue(weakEvent)!;

        // Create a mock listener
        var mockListener = new Mock<IWeakListener<TestEventArgs>>();

        // Add a real weak reference to the list
        var weakRef = new WeakReference<IWeakListener<TestEventArgs>>(mockListener.Object);
        handlers.Add(weakRef);

        // Verify we have one handler
        Assert.AreEqual(1, handlers.Count, "Should have 1 handler before test");

        // Now create a new list with our own "dead" weak reference implementation
        var newHandlersList = new List<WeakReference<IWeakListener<TestEventArgs>>>();

        // Create a proxy for handling TryGetTarget calls - to track and control behavior
        bool tryGetTargetCalled = false;
        handlers[0] = new WeakReference<IWeakListener<TestEventArgs>>(mockListener.Object);

        // Use reflection to replace the handlers field with our controlled list
        // First, we need to create a mock of the handlers list
        var mockHandlersList = new Mock<List<WeakReference<IWeakListener<TestEventArgs>>>>();

        // Set up the RemoveAll method to simulate removing dead references
        mockHandlersList.Setup(l => l.RemoveAll(It.IsAny<Predicate<WeakReference<IWeakListener<TestEventArgs>>>>()))
                       .Callback<Predicate<WeakReference<IWeakListener<TestEventArgs>>>>(predicate =>
                       {
                           // Simulate the RemoveAll by calling the predicate with our weak reference
                           // If the predicate returns true, it means the item would be removed
                           tryGetTargetCalled = true;

                           // The cleanup should try to remove the item since TryGetTarget will return false
                           Assert.IsTrue(predicate(weakRef), "Predicate should return true for dead references");
                       })
                       .Returns(1); // Simulate that 1 item was removed

        mockHandlersList.Setup(l => l.Count).Returns(0); // After removal, count should be 0

        // Replace the real handlers list with our mock
        fieldInfo.SetValue(weakEvent, mockHandlersList.Object);

        // Act: Raise the event which should clean up dead references
        weakEvent.Raise(this, new TestEventArgs { Value = 42 });

        // Assert: Verify the RemoveAll was called
        Assert.IsTrue(tryGetTargetCalled, "RemoveAll should have been called");
        mockHandlersList.Verify(l => l.RemoveAll(It.IsAny<Predicate<WeakReference<IWeakListener<TestEventArgs>>>>()), Times.Once);
    }

    [TestMethod]
    public void WeakEvent_ShouldSupportStaticHandlers()
    {
        // Arrange
        var weakEvent = new WeakEvent<TestEventArgs>();
        var staticListener = new StaticEventListener();
        StaticHandlerCounter.Count = 0;

        // Act
        weakEvent.AddListener(staticListener);
        weakEvent.Raise(this, new TestEventArgs { Value = 42 });

        // Assert
        Assert.AreEqual(1, StaticHandlerCounter.Count, "Static handler should be called");

        // Force garbage collection (shouldn't affect static methods since we hold a reference)
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
        GC.WaitForPendingFinalizers();

        // Reset counter
        StaticHandlerCounter.Count = 0;

        // Raise again
        weakEvent.Raise(this, new TestEventArgs { Value = 100 });

        // Assert static handler still works
        Assert.AreEqual(1, StaticHandlerCounter.Count, "Static handler should still work after GC");
    }

    [TestMethod]
    public void WeakEvent_ShouldHandleMultipleRaises()
    {
        // Arrange
        var weakEvent = new WeakEvent<TestEventArgs>();
        var listener = new EventListener();

        // Act
        weakEvent.AddListener(listener);

        // Raise multiple times
        for (int i = 0; i < 5; i++)
        {
            weakEvent.Raise(this, new TestEventArgs { Value = i });
        }

        // Assert
        Assert.AreEqual(5, listener.CallCount, "Listener should be called five times");
        Assert.AreEqual(4, listener.LastValue, "Last value should be 4");
    }

    [TestMethod]
    public void WeakEvent_ShouldSupportDynamicallyAddingAndRemovingHandlers()
    {
        // Arrange
        var weakEvent = new WeakEvent<TestEventArgs>();
        var listener1 = new EventListener();
        var listener2 = new EventListener();
        var listener3 = new EventListener();

        // Act - add first listener
        weakEvent.AddListener(listener1);
        weakEvent.Raise(this, new TestEventArgs { Value = 1 });

        // Add second listener
        weakEvent.AddListener(listener2);
        weakEvent.Raise(this, new TestEventArgs { Value = 2 });

        // Remove first listener
        weakEvent.RemoveListener(listener1);
        weakEvent.Raise(this, new TestEventArgs { Value = 3 });

        // Add third listener
        weakEvent.AddListener(listener3);
        weakEvent.Raise(this, new TestEventArgs { Value = 4 });

        // Assert
        Assert.AreEqual(2, listener1.CallCount, "First listener should be called twice");
        Assert.AreEqual(3, listener2.CallCount, "Second listener should be called three times");
        Assert.AreEqual(1, listener3.CallCount, "Third listener should be called once");

        Assert.AreEqual(2, listener1.LastValue, "First listener should have last value 2");
        Assert.AreEqual(4, listener2.LastValue, "Second listener should have last value 4");
        Assert.AreEqual(4, listener3.LastValue, "Third listener should have last value 4");
    }

    [TestMethod]
    public void WeakEvent_ShouldWorkWithAnonymousHandlers()
    {
        // Arrange
        var weakEvent = new WeakEvent<TestEventArgs>();
        int capturedValue = 0;
        var anonymousListener = new AnonymousEventListener((sender, args) => capturedValue = args.Value);

        // Act
        weakEvent.AddListener(anonymousListener);
        weakEvent.Raise(this, new TestEventArgs { Value = 42 });

        // Assert
        Assert.AreEqual(42, capturedValue, "Anonymous handler should be called");
    }

    [TestMethod]
    public void WeakEvent_ShouldHandleRaisingWhenNoHandlersRegistered()
    {
        // Arrange
        var weakEvent = new WeakEvent<TestEventArgs>();

        // Act & Assert - should not throw
        weakEvent.Raise(this, new TestEventArgs { Value = 42 });
    }

    // Helper for testing static handlers
    private static class StaticHandlerCounter
    {
        public static int Count { get; set; }
    }
}
