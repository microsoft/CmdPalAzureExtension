// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace AzureExtension.Controls.Forms;

public interface IAzureForm
{
    Dictionary<string, string> TemplateSubstitutions { get; }
}
