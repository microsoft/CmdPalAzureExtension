// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Services.WebApi;

namespace AzureExtension.Client;

public class ConnectionResult
{
    public ResultType Result { get; private set; } = ResultType.Unknown;

    public ErrorType Error { get; private set; } = ErrorType.Unknown;

    public Exception? Exception
    {
        get; private set;
    }

    public Uri? UriValue
    {
        get; private set;
    }

    public VssHttpClientBase? T
    {
        get; private set;
    }

    public IVssConnection? Connection
    {
        get; private set;
    }

    public bool AttemptSilentReauthorization
    {
        get;
        set;
    }

    public ConnectionResult(Uri? uri, VssHttpClientBase? type, IVssConnection? connection)
    {
        Result = ResultType.Success;
        Error = ErrorType.None;
        UriValue = uri;
        T = type;
        Connection = connection;
    }

    public ConnectionResult(ResultType result, ErrorType error, bool attemptSilentReauthorization, Exception? exception = null)
    {
        Result = result;
        Error = error;
        AttemptSilentReauthorization = attemptSilentReauthorization;
        Exception = exception;
    }
}
