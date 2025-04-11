// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls.Forms;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Pages
{
    public class SavePullRequestSearchPage : ContentPage
    {
        private SavePullRequestSearchForm _savePullRequestSearchForm;

        public SavePullRequestSearchPage(SavePullRequestSearchForm savePullRequestSearchForm)
        {
            Title = "Save Pull Request";
            Icon = new IconInfo("\uecc8");
            _savePullRequestSearchForm = savePullRequestSearchForm;
        }

        public override IContent[] GetContent()
        {
            return new IContent[]
            {
                _savePullRequestSearchForm,
            };
        }
    }
}
