using LocationTrackerMAUI.Models;

namespace LocationTrackerMAUI.Services;

/// <summary>
/// Raised every time a new GPS reading is captured while tracking is active.
/// </summary>
public class LocationCapturedEventArgs : EventArgs
{
    public LocationCapturedEventArgs(LocationPoint point) => Point = point;

    public LocationPoint Point { get; }
}

/// <summary>
/// Owns the periodic GPS polling loop. It wraps .NET MAUI's
/// <see cref="Geolocation"/> API, requests the runtime permission it needs,
/// and raises <see cref="LocationCaptured"/> on a timer so the UI layer can
/// save each point and redraw the heat map.
/// </summary>
public class LocationTrackingService
{
    /// <summary>How often a new GPS fix is requested while tracking.</summary>
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(15);

    private IDispatcherTimer? _timer;
    private bool _isTracking;

    /// <summary>Raised whenever a new point is captured.</summary>
    public event EventHandler<LocationCapturedEventArgs>? LocationCaptured;

    /// <summary>True while the polling timer is running.</summary>
    public bool IsTracking => _isTracking;

    /// <summary>
    /// Requests the location permission (if not already granted) and starts
    /// polling the device's GPS on <see cref="PollingInterval"/>.
    /// </summary>
    /// <returns>True if tracking started; false if permission was denied.</returns>
    public async Task<bool> StartAsync()
    {
        if (_isTracking)
        {
            return true;
        }

        var permissionGranted = await EnsureLocationPermissionAsync();
        if (!permissionGranted)
        {
            return false;
        }

        // Capture an initial fix immediately so the user sees a point on
        // the map right away instead of waiting a full interval.
        await CaptureLocationAsync();

        _timer = Application.Current?.Dispatcher.CreateTimer();
        if (_timer is not null)
        {
            _timer.Interval = PollingInterval;
            _timer.Tick += async (_, _) => await CaptureLocationAsync();
            _timer.Start();
        }

        _isTracking = true;
        return true;
    }

    /// <summary>Stops the polling timer. Safe to call even if not tracking.</summary>
    public void Stop()
    {
        if (_timer is not null)
        {
            _timer.Stop();
            _timer = null;
        }

        _isTracking = false;
    }

    /// <summary>
    /// Requests a single GPS fix and raises <see cref="LocationCaptured"/>.
    /// Any failure (GPS disabled, timeout, etc.) is swallowed here so a
    /// single bad reading does not crash the polling loop; it is simply
    /// skipped and tried again on the next tick.
    /// </summary>
    private async Task CaptureLocationAsync()
    {
        try
        {
            var request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10));
            var location = await Geolocation.Default.GetLocationAsync(request);

            if (location is null)
            {
                return;
            }

            var point = new LocationPoint
            {
                Latitude = location.Latitude,
                Longitude = location.Longitude,
                TimestampUtc = DateTime.UtcNow
            };

            LocationCaptured?.Invoke(this, new LocationCapturedEventArgs(point));
        }
        catch (FeatureNotSupportedException)
        {
            // Location services are not supported on this device/emulator.
        }
        catch (FeatureNotEnabledException)
        {
            // The user has disabled location services at the OS level.
        }
        catch (PermissionException)
        {
            // Permission was revoked after StartAsync() ran; stop tracking.
            Stop();
        }
        catch (Exception)
        {
            // Any other transient error (timeout, no signal, etc.) - ignore
            // and try again on the next polling tick.
        }
    }

    /// <summary>
    /// Checks the current location permission status and, if needed,
    /// prompts the user for "when in use" access.
    /// </summary>
    private static async Task<bool> EnsureLocationPermissionAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        }

        return status == PermissionStatus.Granted;
    }
}
