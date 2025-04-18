// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace AzureExtension.Helpers;

public static class AzureIcon
{
    static AzureIcon()
    {
        IconDictionary = new Dictionary<string, string>
            {
                { "logo_dark", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "AzureExtensionDark.png") },
                { "logo_light", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "AzureLogo_Light.png") },
                { "issue", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "issues.png") },
                { "pr", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "pulls.png") },
                { "release", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "releases.png") },
                { "logo", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "AzureExtensionDark.png") },
                { "Issues", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "issues.png") },
                { "PullRequests", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "pulls.png") },
                { "IssuesAndPullRequests", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "gh_logo.jpg") },
            };
    }

    public static Dictionary<string, string> IconDictionary { get; private set; }

    public static string GetBase64Icon(string iconKey)
    {
        if (IconDictionary.TryGetValue(iconKey, out var iconPath))
        {
            var bytes = File.ReadAllBytes(iconPath);
            return Convert.ToBase64String(bytes);
        }

        return string.Empty;
    }

    private static string GetIconForType(string? workItemType)
    {
        return workItemType switch
        {
            "Bug" => IconLoader.GetIconAsBase64("Bug.png"),
            "Feature" => IconLoader.GetIconAsBase64("Feature.png"),
            "Issue" => IconLoader.GetIconAsBase64("Issue.png"),
            "Impediment" => IconLoader.GetIconAsBase64("Impediment.png"),
            "Pull Request" => IconLoader.GetIconAsBase64("PullRequest.png"),
            "Task" => IconLoader.GetIconAsBase64("Task.png"),
            _ => IconLoader.GetIconAsBase64("ADO.png"),
        };
    }

    private static string GetIconForStatusState(string? statusState)
    {
        return statusState switch
        {
            "Closed" or "Completed" => IconLoader.GetIconAsBase64("StatusGreen.png"),
            "Committed" or "Resolved" or "Started" => IconLoader.GetIconAsBase64("StatusBlue.png"),
            _ => IconLoader.GetIconAsBase64("StatusGray.png"),
        };
    }
}
