// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureExtension.Controls.DataTransfer;

internal class PipelineDefinitionSearchCandidate : IPipelineDefinitionSearch
{
    public long InternalId { get; set; }

    public string Url { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool IsTopLevel { get; set; }
}
