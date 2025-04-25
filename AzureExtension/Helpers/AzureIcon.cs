// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Helpers;

public static class AzureIcon
{
    static AzureIcon()
    {
        IconDictionary = new Dictionary<string, IconInfo>
        {
                { "logo_dark", IconHelpers.FromRelativePath(@"Assets\AzureExtensionDark.png") },
                { "logo_light", IconHelpers.FromRelativePath(@"Assets\AzureLogo_Light.png") },
                { "issue", IconHelpers.FromRelativePath(@"Assets\issues.png") },
                { "pr", IconHelpers.FromRelativePath(@"Assets\pulls.png") },
                { "release", IconHelpers.FromRelativePath(@"Assets\releases.png") },
                { "logo", IconHelpers.FromRelativePath(@"Assets\AzureExtensionDark.png") },
                { "Bug", IconHelpers.FromRelativePath(@"Assets\Bug.png") },
                { "Feature", IconHelpers.FromRelativePath(@"Assets\Feature.png") },
                { "Issue", IconHelpers.FromRelativePath(@"Assets\Issue.png") },
                { "Impediment", IconHelpers.FromRelativePath(@"Assets\Impediment.png") },
                { "PullRequest", IconHelpers.FromRelativePath(@"Assets\PullRequest.png") },
                { "Task", IconHelpers.FromRelativePath(@"Assets\Task.png") },
                { "Deliverable", IconHelpers.FromRelativePath(@"Assets\Deliverable.svg") },
                { "StatusGreen", IconHelpers.FromRelativePath(@"Assets\StatusGreen.png") },
                { "StatusBlue", IconHelpers.FromRelativePath(@"Assets\StatusBlue.png") },
                { "StatusGray", IconHelpers.FromRelativePath(@"Assets\StatusGray.png") },
                { "StatusRed", IconHelpers.FromRelativePath(@"Assets\StatusRed.png") },
                { "StatusYellow", IconHelpers.FromRelativePath(@"Assets\StatusYellow.png") },
                { "StatusPurple", IconHelpers.FromRelativePath(@"Assets\StatusPurple.png") },
                { "StatusOrange", IconHelpers.FromRelativePath(@"Assets\StatusOrange.png") },
                { "Query", IconHelpers.FromRelativePath(@"Assets\Query.svg") },
        };
    }

    public static Dictionary<string, IconInfo> IconDictionary { get; private set; }

    private static string GetIconForStatusState(string? statusState)
    {
        return statusState switch
        {
            "Closed" or "Completed" => IconLoader.GetIconAsBase64("StatusGreen.png"),
            "Committed" or "Resolved" or "Started" => IconLoader.GetIconAsBase64("StatusBlue.png"),
            _ => IconLoader.GetIconAsBase64("StatusGray.png"),
        };
    }

    public static IconInfo GetIconForType(string? workItemType)
    {
        string iconKey = workItemType switch
        {
            "Bug" => "Bug",
            "Feature" => "Feature",
            "Issue" => "Issue",
            "Impediment" => "Impediment",
            "Pull Request" => "PullRequest",
            "Task" => "Task",
            "Deliverable" => "Deliverable",
            _ => "logo",
        };

        if (IconDictionary.TryGetValue(iconKey, out var iconInfo))
        {
            return iconInfo;
        }

        // Return a default icon if the specified type isn't found
        return IconDictionary["logo"];
    }

    // Converts icon path to a base64 string for adaptive cards
    // CmdPal UI uses IconInfo
    public static string GetBase64Icon(string iconPath)
    {
        if (!string.IsNullOrEmpty(iconPath))
        {
            var bytes = File.ReadAllBytes(iconPath);
            return Convert.ToBase64String(bytes);
        }

        return string.Empty;
    }
}
