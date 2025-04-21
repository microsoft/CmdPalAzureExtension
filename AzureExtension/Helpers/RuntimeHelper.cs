// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Windows.Win32;
using Windows.Win32.Foundation;

namespace AzureExtension.Helpers;

public static class RuntimeHelper
{
    public static bool IsMSIX
    {
        get
        {
            uint length = 0;
            return PInvoke.GetCurrentPackageFullName(ref length, null) != WIN32_ERROR.APPMODEL_ERROR_NO_PACKAGE;
        }
    }
}
