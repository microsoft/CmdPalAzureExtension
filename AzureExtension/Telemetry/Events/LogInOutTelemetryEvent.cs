// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Tracing;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;

namespace AzureExtension.Telemetry.Events;

[EventData]
public class LogInOutTelemetryEvent : TelemetryEventBase
{
#if TELEMETRYEVENTSOURCE_PUBLIC
    [CLSCompliant(false)]
    public
#else
    internal
#endif
    override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServicePerformance;

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
    }
}
