// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.DataManager;
using AzureExtension.DataModel;
using Serilog;

namespace AzureExtension;

public partial class AzureDataManager
{
    // This is how frequently the DataStore update occurs.
    private static readonly TimeSpan _updateInterval = TimeSpan.FromMinutes(5);
    private static DateTime _lastUpdateTime = DateTime.MinValue;

    public async Task Update()
    {
        // Only update per the update interval.
        // This is intended to be dynamic in the future.
        if (DateTime.UtcNow - _lastUpdateTime < _updateInterval)
        {
            return;
        }

        try
        {
            await UpdateDeveloperPullRequests();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Update failed unexpectedly.");
        }

        _lastUpdateTime = DateTime.UtcNow;
    }

    public async Task UpdateDeveloperPullRequests()
    {
        var log = Log.ForContext("SourceContext", $"UpdateDeveloperPullRequests");
        log.Debug($"Executing UpdateDeveloperPullRequests");

        /*
        ** Commenting this out as it is a SRP violation and introducts a cyclic dependency.
        var cacheManager = CacheManager.GetInstance();
        if (cacheManager.UpdateInProgress)
        {
            log.Information("Cache is being updated, skipping Developer Pull Request Update");
            return;
        }
        */

        var identifier = Guid.NewGuid();
        using var dataManager = new AzureDataManager(identifier.ToString(), _developerIdProvider) ?? throw new DataStoreInaccessibleException();
        await dataManager.UpdatePullRequestsForLoggedInDeveloperIdsAsync(null, identifier);

        // Show any new notifications that were created from the pull request update.
        var notifications = dataManager.GetNotifications();
        foreach (var notification in notifications)
        {
            // Show notifications for failed checkruns for Developer users.
            if (notification.Type == NotificationType.PullRequestRejected || notification.Type == NotificationType.PullRequestApproved)
            {
                notification.ShowToast();
            }
        }
    }
}
