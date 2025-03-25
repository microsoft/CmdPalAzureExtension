// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace AzureExtension.Widgets;

public enum WidgetDataState
{
    Unknown,
    Requested,      // Request is out, waiting on a response. Current data is stale.
    Okay,           // Received and updated data, stable state.
    FailedUpdate,   // Failed updating data.
    FailedRead,     // Failed to read the data.
    Disposed,       // DataManager has been disposed and should not be used.
}
