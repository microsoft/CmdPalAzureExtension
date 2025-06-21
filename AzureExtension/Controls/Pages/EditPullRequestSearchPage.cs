// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls.Forms;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls;

internal sealed partial class EditPullRequestSearchPage : ContentPage
{
    private readonly IResources _resources;
    private readonly SavePullRequestSearchForm _savePullRequestSearchForm;

    public EditPullRequestSearchPage(IResources resources, SavePullRequestSearchForm savePullRequestSearchForm)
    {
        _resources = resources;
        _savePullRequestSearchForm = savePullRequestSearchForm;
        Title = _resources.GetResource("Pages_EditPullRequestSearch");
        Name = Title; // Title is for the Page, Name is for the Command
        Icon = IconLoader.GetIcon("Edit");
    }

    public override IContent[] GetContent()
    {
        return [_savePullRequestSearchForm];
    }
}
