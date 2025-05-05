// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Jeffijoe.MessageFormat;
using Serilog;

namespace AzureExtension.Helpers;

public sealed class TimeSpanHelper
{
    private readonly IResources _resources;

    public TimeSpanHelper(IResources resources)
    {
        _resources = resources;
    }

    public string TimeSpanToDisplayString(TimeSpan timeSpan, ILogger? log = null)
    {
        if (timeSpan.TotalSeconds < 1)
        {
            return _resources.GetResource("TimeNow", log);
        }

        if (timeSpan.TotalMinutes < 1)
        {
            return MessageFormatter.Format(_resources.GetResource("TimeSecondsAgo", log), new { seconds = timeSpan.Seconds });
        }

        if (timeSpan.TotalHours < 1)
        {
            return MessageFormatter.Format(_resources.GetResource("TimeMinutesAgo", log), new { minutes = timeSpan.Minutes });
        }

        if (timeSpan.TotalDays < 1)
        {
            return MessageFormatter.Format(_resources.GetResource("TimeHoursAgo", log), new { hours = timeSpan.Hours });
        }

        return MessageFormatter.Format(_resources.GetResource("TimeDaysAgo", log), new { days = timeSpan.Days });
    }

    internal string DateTimeOffsetToDisplayString(DateTimeOffset? dateTime, ILogger? log)
    {
        if (dateTime == null)
        {
            return _resources.GetResource("TimeUnknownTime", log);
        }

        return TimeSpanToDisplayString(DateTime.UtcNow - dateTime.Value.DateTime, log);
    }
}
