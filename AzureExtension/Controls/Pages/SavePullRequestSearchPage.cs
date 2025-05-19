// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls.Forms;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Pages;

public class SavePullRequestSearchPage : ContentPage
{
    private readonly StatusMessage _statusMessage;

    private readonly SavePullRequestSearchForm _savePullRequestSearchForm;

    private readonly IResources _resources;

    public SavePullRequestSearchPage(SavePullRequestSearchForm savePullRequestSearchForm, StatusMessage statusMessage, IResources resources)
    {
        _resources = resources;
        Title = _resources.GetResource("Pages_SavePullRequestSearch_Title");
        Icon = IconLoader.GetIcon("Add");
        _savePullRequestSearchForm = savePullRequestSearchForm;
        _statusMessage = statusMessage;

        FormEventHelper.WireFormEvents(_savePullRequestSearchForm, this, _statusMessage, _resources.GetResource("Pages_SavePullRequestSearch_SuccessMessage"), _resources.GetResource("Pages_SavePullRequestSearch_FailureMessage"));

        ExtensionHost.HideStatus(_statusMessage);
    }

    public override IContent[] GetContent()
    {
        ExtensionHost.HideStatus(_statusMessage);
        return [_savePullRequestSearchForm];
    }
}
