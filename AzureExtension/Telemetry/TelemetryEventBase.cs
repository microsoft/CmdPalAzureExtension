// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.Tracing;
using Microsoft.Diagnostics.Telemetry.Internal;

namespace AzureExtension.Telemetry;

/// <summary>
/// Base class for all telemetry events to ensure they are properly tagged.
/// </summary>
/// <remarks>
/// The public properties of each event are logged in the telemetry.
/// We should not change an event's properties, as that could break the processing of that event's data.
/// </remarks>
[EventData]
public abstract class TelemetryEventBase
{
    /// <summary>
    /// Gets the privacy datatype tag for the telemetry event.
    /// </summary>
    internal abstract PartA_PrivTags PartA_PrivTags
    {
        get;
    }

    /// <summary>
    /// Replaces all the strings in this event that may contain PII using the provided function.
    /// </summary>
    /// <remarks>
    /// This is called by <see cref="ITelemetry"/> before logging the event.
    /// It is the responsibility of each event to ensure we replace all strings with possible PII;
    /// we ensure we at least consider this by forcing to implement this.
    /// </remarks>
    /// <param name="replaceSensitiveStrings">
    /// A function that replaces all the sensitive strings in a given string with tokens
    /// </param>
    public abstract void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings);
}
