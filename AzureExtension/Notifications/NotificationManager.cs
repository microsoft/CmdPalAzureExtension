// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Windows.AppNotifications;
using Serilog;

namespace AzureExtension.Notifications;

public class NotificationManager
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(NotificationManager));

    private bool _isRegistered;

    public NotificationManager(Windows.Foundation.TypedEventHandler<AppNotificationManager, AppNotificationActivatedEventArgs> handler)
    {
        AppNotificationManager.Default.NotificationInvoked += handler;
        AppNotificationManager.Default.Register();
        _isRegistered = true;
        _log.Debug($"NotificationManager created and registered.");
    }

    ~NotificationManager()
    {
        Unregister();
    }

    public void Unregister()
    {
        if (_isRegistered)
        {
            AppNotificationManager.Default.Unregister();
            _isRegistered = false;
            _log.Debug($"NotificationManager unregistered.");
        }
    }
}
