// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Serilog;

namespace AzureExtension.Client;

// Validation layer to help parsing GitHub URL.
public static class Validation
{
    private static readonly Lazy<ILogger> _logger = new(() => Serilog.Log.ForContext("SourceContext", nameof(Validation)));

    private static readonly ILogger _log = _logger.Value;

    public static bool IsValidHttpUri(string uriString, out Uri? uri)
    {
        return Uri.TryCreate(uriString, UriKind.Absolute, out uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
