// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.IdentityModel.Abstractions;
using Serilog;

namespace AzureExtension.Account;

// The below class is used in MSAL.NET logging for better diagnosability of issues (see more here: https://learn.microsoft.com/en-us/entra/msal/dotnet/advanced/exceptions/msal-logging)
public class MSALLogger(EventLogLevel minLogLevel = EventLogLevel.LogAlways) : IIdentityLogger
{
    private readonly EventLogLevel _minLogLevel = minLogLevel;
    private readonly ILogger _log = Serilog.Log.ForContext("SourceContext", nameof(MSALLogger));

    public bool IsEnabled(EventLogLevel eventLogLevel)
    {
        return eventLogLevel >= _minLogLevel;
    }

    public void Log(LogEntry entry)
    {
        switch (entry.EventLogLevel)
        {
            case EventLogLevel.Verbose:
                _log.Verbose($"{entry.Message}");
                break;
            case EventLogLevel.Critical:
                _log.Fatal($"{entry.Message}");
                break;
            case EventLogLevel.Error:
                _log.Error($"{entry.Message}");
                break;
            case EventLogLevel.Warning:
                // MSAL logging is even spammy with warnings.
                _log.Debug($"{entry.Message}");
                break;
            case EventLogLevel.Informational:
                // The MSAL Logging is very spammy, information is very common.
                _log.Verbose($"{entry.Message}");
                break;
        }
    }
}
