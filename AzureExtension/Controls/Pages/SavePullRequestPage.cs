// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls.Forms;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Pages
{
    public class SavePullRequestPage : ContentPage
    {
        private SavePullRequestForm _savePullRequestForm;

        public SavePullRequestPage(SavePullRequestForm savePullRequestForm)
        {
            Title = "Save Pull Request";
            Icon = new IconInfo("\uecc8");
            _savePullRequestForm = savePullRequestForm;
        }

        public override IContent[] GetContent()
        {
            return new IContent[]
            {
                _savePullRequestForm,
            };
        }
    }
}
