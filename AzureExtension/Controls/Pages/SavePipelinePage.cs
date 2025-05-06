// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls.Forms;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Pages
{
    public class SavePipelinePage : ContentPage
    {
        public override IContent[] GetContent()
        {
            var content = new SavePipelineDefinitionForm();
            return new[] { content };
        }
    }
}
