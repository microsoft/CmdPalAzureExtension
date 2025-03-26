// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace AzureExtension.Helpers;

public class SignInStatusChangedEventArgs : EventArgs
{
    public bool IsSignedIn { get; }

    public Exception? Error { get; }

    public SignInStatusChangedEventArgs(bool isSignedIn, Exception? error = null)
    {
        IsSignedIn = isSignedIn;
        Error = error;
    }
}
