// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls.Forms;

public class SavePipelineDefinitionForm : FormContent
{
    private readonly IResources _resources;

    private string IsTopLevelChecked => "false";

    public SavePipelineDefinitionForm(IResources resources)
    {
        _resources = resources;
    }

    public Dictionary<string, string> TemplateSubstitutions => new()
{
    { "{{SaveQueryFormTitle}}", _resources.GetResource(string.IsNullOrEmpty("SavedPipeline.Name") ? "Forms_Save_Query" : "Forms_Edit_Query") },
    { "{{SavedQueryString}}", string.Empty }, // pipeline URL
    { "{{SavedQueryName}}", string.Empty }, // pipeline name
    { "{{EnteredQueryErrorMessage}}", _resources.GetResource("Forms_SaveQuery_TemplateEnteredQueryError") },
    { "{{EnteredQueryLabel}}", _resources.GetResource("Forms_SaveQuery_TemplateEnteredQueryLabel") },
    { "{{NameLabel}}", _resources.GetResource("Forms_SaveQuery_TemplateNameLabel") },
    { "{{NameErrorMessage}}", _resources.GetResource("Forms_SaveQuery_TemplateNameError") },
    { "{{IsTopLevelTitle}}", _resources.GetResource("Forms_SaveQueryTemplate_IsTopLevelTitle") },
    { "{{IsTopLevel}}", IsTopLevelChecked },
    { "{{SaveQueryActionTitle}}", _resources.GetResource("Forms_SaveQuery_TemplateSaveQueryActionTitle") },
};

    public override string TemplateJson => TemplateHelper.LoadTemplateJsonFromTemplateName("SaveQuery", TemplateSubstitutions);
}
