// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.UI.Xaml;
using Serilog;

namespace AzureExtension.Helpers;

public class IconLoader
{
    private static readonly Dictionary<string, (string LightModePath, string DarkModePath)> _filePathDictionary = new();
    private static readonly Dictionary<string, (string LightModeBase64, string DarkModeBase64)> _base64ImageRegistry = new();
    private static Dictionary<string, IconInfo> _iconDictionary;

    static IconLoader()
    {
        _filePathDictionary = new Dictionary<string, (string LightModePath, string DarkModePath)>
        {
            { "Logo", (@"Assets\AzureLogo_Light.png", @"Assets\AzureExtensionDark.png") },
            { "Bug", (@"Assets\Bug.png", @"Assets\Bug.png") },
            { "ChangeRequest", (@"Assets\ChangeRequest.png", @"Assets\ChangeRequest.png") },
            { "Deliverable", (@"Assets\Deliverable.svg", @"Assets\Deliverable.svg") },
            { "Epic", (@"Assets\Epic.png", @"Assets\Epic.png") },
            { "Feature", (@"Assets\Feature.png", @"Assets\Feature.png") },
            { "Impediment", (@"Assets\Impediment.png", @"Assets\Impediment.png") },
            { "Issue", (@"Assets\Issue.png", @"Assets\Issue.png") },
            { "ProductBacklogItem", (@"Assets\ProductBacklogItem.png", @"Assets\ProductBacklogItem.png") },
            { "PullRequest", (@"Assets\PullRequest.png", @"Assets\PullRequest.png") },
            { "PullRequestApproved", (@"Assets\PullRequestApproved.png", @"Assets\PullRequestApproved.png") },
            { "PullRequestRejected", (@"Assets\PullRequestRejected.png", @"Assets\PullRequestRejected.png") },
            { "PullRequestReviewNotStarted", (@"Assets\PullRequestReviewNotStarted.png", @"Assets\PullRequestReviewNotStarted.png") },
            { "PullRequestWaiting", (@"Assets\PullRequestWaiting.png", @"Assets\PullRequestWaiting.png") },
            { "Query", (@"Assets\Query.svg", @"Assets\Query.svg") },
            { "Requirement", (@"Assets\Requirement.png", @"Assets\Requirement.png") },
            { "Review", (@"Assets\Review.png", @"Assets\Review.png") },
            { "Risk", (@"Assets\Risk.png", @"Assets\Risk.png") },
            { "StatusBlue", (@"Assets\StatusBlue.png", @"Assets\StatusBlue.png") },
            { "StatusGray", (@"Assets\StatusGray.png", @"Assets\StatusGray.png") },
            { "StatusGreen", (@"Assets\StatusGreen.png", @"Assets\StatusGreen.png") },
            { "StatusOrange", (@"Assets\StatusOrange.png", @"Assets\StatusOrange.png") },
            { "StatusRed", (@"Assets\StatusRed.png", @"Assets\StatusRed.png") },
            { "Task", (@"Assets\Task.png", @"Assets\Task.png") },
            { "TaskGroup", (@"Assets\TaskGroup.png", @"Assets\TaskGroup.png") },
            { "TestCase", (@"Assets\TestCase.png", @"Assets\TestCase.png") },
            { "TestPlan", (@"Assets\TestPlan.png", @"Assets\TestPlan.png") },
            { "TestSuite", (@"Assets\TestSuite.png", @"Assets\TestSuite.png") },
            { "UserStory", (@"Assets\UserStory.png", @"Assets\UserStory.png") },
        };

        _iconDictionary = _filePathDictionary.ToDictionary(
            kvp => kvp.Key,
            kvp => IconHelpers.FromRelativePaths(kvp.Value.LightModePath, kvp.Value.DarkModePath));
    }

    public static IconInfo GetIcon(string key)
    {
        if (_iconDictionary.TryGetValue(key, out var iconInfo))
        {
            return iconInfo;
        }

        return _iconDictionary["Logo"];
    }

    public static string GetIconAsBase64(string key, string mode = "dark")
    {
        var log = Log.ForContext("SourceContext", nameof(IconLoader));
        log.Debug($"Asking for icon: {key} in {mode} mode");

        if (!_filePathDictionary.TryGetValue(key, out var paths))
        {
            log.Warning($"Key '{key}' not found in file path dictionary.");
            return string.Empty;
        }

        if (!_base64ImageRegistry.TryGetValue(key, out var base64Values))
        {
            var lightModeBase64 = ConvertIconToDataString(paths.LightModePath);
            var darkModeBase64 = ConvertIconToDataString(paths.DarkModePath);
            base64Values = (lightModeBase64, darkModeBase64);
            _base64ImageRegistry[key] = base64Values;

            log.Debug($"The icon {key} was converted and stored for both light and dark modes.");
        }

        return Application.Current.RequestedTheme == ApplicationTheme.Light ? base64Values.LightModeBase64 : base64Values.DarkModeBase64;
    }

    private static string ConvertIconToDataString(string filePath)
    {
        var fullPath = Path.Combine(AppContext.BaseDirectory, filePath);
        var imageData = Convert.ToBase64String(File.ReadAllBytes(fullPath));
        return imageData;
    }
}
