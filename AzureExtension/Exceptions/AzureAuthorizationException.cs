﻿// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace AzureExtension;

public class AzureAuthorizationException : Exception
{
    public AzureAuthorizationException()
    {
    }

    public AzureAuthorizationException(string message)
        : base(message)
    {
    }
}
