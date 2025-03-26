// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommandPaletteAzureExtension.Helpers;
using Jeffijoe.MessageFormat;
using Serilog;

namespace AzureExtension.Helpers;

internal sealed class TimeSpanHelper
{
    public static string TimeSpanToDisplayString(TimeSpan timeSpan, ILogger? log = null)
    {
        if (timeSpan.TotalSeconds < 1)
        {
            return Resources.GetResource("Widget_Template_Now", log);
        }

        if (timeSpan.TotalMinutes < 1)
        {
            return MessageFormatter.Format(Resources.GetResource("Widget_Template_SecondsAgo", log), new { seconds = timeSpan.Seconds });
        }

        if (timeSpan.TotalHours < 1)
        {
            return MessageFormatter.Format(Resources.GetResource("Widget_Template_MinutesAgo", log), new { minutes = timeSpan.Minutes });
        }

        if (timeSpan.TotalDays < 1)
        {
            return MessageFormatter.Format(Resources.GetResource("Widget_Template_HoursAgo", log), new { hours = timeSpan.Hours });
        }

        return MessageFormatter.Format(Resources.GetResource("Widget_Template_DaysAgo", log), new { days = timeSpan.Days });
    }

    internal static string DateTimeOffsetToDisplayString(DateTimeOffset? dateTime, ILogger log)
    {
        if (dateTime == null)
        {
            return Resources.GetResource("Widget_Template_UnknownTime", log);
        }

        return TimeSpanToDisplayString(DateTime.UtcNow - dateTime.Value.DateTime, log);
    }
}
