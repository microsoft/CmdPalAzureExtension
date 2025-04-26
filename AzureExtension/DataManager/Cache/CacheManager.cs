// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Serilog;

namespace AzureExtension.DataManager.Cache;

public sealed class CacheManager : IDisposable, ICacheManager
{
    public static readonly TimeSpan UpdateInterval = TimeSpan.FromMinutes(10);

    public static readonly TimeSpan RefreshCooldown = TimeSpan.FromMinutes(3);

    // Lock to be used everytime we want to check or update the state of
    // the CacheManager.
    private readonly SemaphoreSlim _stateSemaphore = new(1, 1);

    private readonly ILogger _logger;

    public CacheManagerState State { get; set; }

    public CacheManagerState IdleState { get; private set; }

    public CacheManagerState RefreshingState { get; private set; }

    public CacheManagerState PeriodicUpdatingState { get; private set; }

    public CacheManagerState PendingRefreshState { get; private set; }

    private readonly IDataUpdateService _dataUpdateService;

    private CancellationTokenSource _cancelSource;

    // Variables to control the state of the CacheManager
    // If there is a current update in progress
    public DataUpdateParameters? CurrentUpdateParameters { get; internal set; }

    public bool NeverUpdated => LastUpdated == DateTime.MinValue;

    // Time of the last update. This is updated by the
    // Cache Manager whe it receives an update complete event.
    public DateTime LastUpdated { get => GetLastUpdated(); private set => SetLastUpdated(value); }

    public event CacheManagerUpdateEventHandler? OnUpdate;

    private DataUpdater DataUpdater { get; set; }

    public DateTime LastUpdateTime { get; set; } = DateTime.MinValue;

    public CacheManager(IDataUpdateService dataUpdateService)
    {
        _dataUpdateService = dataUpdateService;
        _dataUpdateService.OnUpdate += HandleDataManagerUpdate;

        DataUpdater = new DataUpdater(PeriodicUpdate);
        _cancelSource = new CancellationTokenSource();
        _logger = Log.Logger.ForContext("SourceContext", nameof(CacheManager));

        // Setting states
        IdleState = new IdleState(this);
        RefreshingState = new RefreshingState(this);
        PeriodicUpdatingState = new PeriodicUpdatingState(this);
        PendingRefreshState = new PendingRefreshState(this);
        State = IdleState;
    }

    public void Start()
    {
        _ = DataUpdater.Start();
    }

    public void Stop()
    {
        DataUpdater.Stop();
    }

    public void CancelUpdateInProgress()
    {
        if (!_cancelSource.IsCancellationRequested)
        {
            _logger.Information("Cancelling update.");
            _cancelSource.Cancel();
        }
    }

    public async Task RequestRefresh(DataUpdateParameters parameters)
    {
        if (_dataUpdateService.IsNewOrStaleData(parameters, RefreshCooldown))
        {
            _logger.Information($"Data is new or stale. Requesting refresh.");
            await Refresh(parameters);
        }
    }

    private async Task SemaphoreWrapper(Func<Task> stateProcedure)
    {
        await _stateSemaphore.WaitAsync();
        try
        {
            await stateProcedure();
        }
        finally
        {
            _stateSemaphore.Release();
        }
    }

    // This method is called by the pages to request
    // an instant update of its data.
    public async Task Refresh(DataUpdateParameters parameters)
    {
        await SemaphoreWrapper(async () => await State.Refresh(parameters));
    }

    public async Task PeriodicUpdate()
    {
        await SemaphoreWrapper(async () => await State.PeriodicUpdate());
    }

    public Task Update(DataUpdateParameters parameters)
    {
        _logger.Information($"Starting update of type {parameters.UpdateType}.");

        _cancelSource = new CancellationTokenSource();
        parameters.CancellationToken = _cancelSource.Token;

        switch (parameters.UpdateType)
        {
            case DataUpdateType.PullRequests:
            case DataUpdateType.Query:
                _ = _dataUpdateService.UpdateData(parameters);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(parameters), parameters, null);
        }

        return Task.CompletedTask;
    }

    public void SendUpdateEvent(object? source, CacheManagerUpdateKind kind, Exception? ex = null)
    {
        if (OnUpdate != null)
        {
            _logger.Debug($"Sending update event. Kind: {kind}.");
            OnUpdate.Invoke(source, new CacheManagerUpdateEventArgs(kind, ex));
        }
    }

    private async void HandleDataManagerUpdate(object? source, DataManagerUpdateEventArgs e)
    {
        _logger.Information($"DataManager update: {e.Kind}, {e.Parameters.UpdateType}");
        await SemaphoreWrapper(() =>
        {
            State.HandleDataManagerUpdate(source, e);
            return Task.CompletedTask;
        });

        switch (e.Kind)
        {
            case DataManagerUpdateKind.Success:
                SendUpdateEvent(this, CacheManagerUpdateKind.Updated);
                break;
            case DataManagerUpdateKind.Cancel:
                SendUpdateEvent(this, CacheManagerUpdateKind.Cancel);
                break;
            case DataManagerUpdateKind.Error:
                SendUpdateEvent(this, CacheManagerUpdateKind.Error, e.Exception);
                break;
        }
    }

    private DateTime GetLastUpdated()
    {
        var lastCacheUpdate = _dataUpdateService.LastUpdated;
        if (lastCacheUpdate != null)
        {
            return lastCacheUpdate;
        }

        return DateTime.MinValue;
    }

    private void SetLastUpdated(DateTime time)
    {
        _dataUpdateService.LastUpdated = time;
    }

    // Disposing area
    private bool _disposed;

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            _logger.Debug("Disposing of all cacheManager resources.");

            if (disposing)
            {
                try
                {
                    _logger.Debug("Disposing of all CacheManager resources.");
                    _dataUpdateService.OnUpdate -= HandleDataManagerUpdate;
                    DataUpdater.Dispose();
                    _cancelSource.Dispose();
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Failed disposing");
                }
            }

            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
