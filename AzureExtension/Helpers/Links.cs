// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Services.WebApi;

namespace AzureExtension.Helpers;

public class Links
{
    public static string GetLinkHref(ReferenceLinks links, string linkName)
    {
        if (links == null || links.Links == null
            || !links.Links.ContainsKey(linkName)
            || links.Links[linkName] == null)
        {
            return string.Empty;
        }

        if (links.Links[linkName] is not ReferenceLink linkRef)
        {
            return string.Empty;
        }

        return linkRef.Href;
    }
}
