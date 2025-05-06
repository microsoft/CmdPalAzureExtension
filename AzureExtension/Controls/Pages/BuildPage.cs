// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Pages;

public class BuildPage : ListPage
{
    public override string Title => "Build Page";

    public override string Name => "Build Page"; // Title is for the Page, Name is for the command

    public override IconInfo Icon => IconLoader.GetIcon("Logo");

    public BuildPage()
    {
    }
}
