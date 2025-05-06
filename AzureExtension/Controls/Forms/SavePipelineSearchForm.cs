// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Forms;

public class SavePipelineSearchForm : FormContent
{
    private readonly IResources _resources;

    private string IsTopLevelChecked => "false";

    public SavePipelineSearchForm(IResources resources)
    {
        _resources = resources;
    }

    public Dictionary<string, string> TemplateSubstitutions => new()
    {
        { "{{SavePipelineSearchFormTitle}}", _resources.GetResource(string.IsNullOrEmpty(string.Empty) ? "Forms_Save_PipelineSearch" : "Forms_Edit_PipelineSearch") },
        { "{{SavedPipelineSearchString}}", string.Empty }, // pipeline URL
        { "{{EnteredPipelineSearchErrorMessage}}", _resources.GetResource("Forms_SavePipelineSearch_TemplateEnteredPipelineSearchError") },
        { "{{EnteredPipelineSearchLabel}}", _resources.GetResource("Forms_SavePipelineSearch_TemplateEnteredPipelineSearchLabel") },
        { "{{Forms_SavePipelineSearch_URLPlaceholderSuffix}}", _resources.GetResource("Forms_SavePipelineSearch_URLPlaceholderSuffix") },
        { "{{IsTopLevelTitle}}", _resources.GetResource("Forms_SavePipelineSearch_TemplateIsTopLevelTitle") },
        { "{{IsTopLevel}}", IsTopLevelChecked },
        { "{{SavePipelineSearchActionTitle}}", _resources.GetResource("Forms_SavePipelineSearch_TemplateSavePipelineSearchActionTitle") },
    };

    public override string TemplateJson => TemplateHelper.LoadTemplateJsonFromTemplateName("SavePipelineSearch", TemplateSubstitutions);
}
