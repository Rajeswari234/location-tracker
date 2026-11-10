using LocationTrackerMAUI.Models;
using LocationTrackerMAUI.Services;
using Microsoft.Maui.Maps;

namespace LocationTrackerMAUI;

/// <summary>
/// Code-behind for the single-page UI. Responsibilities:
///   - Start/stop GPS tracking via <see cref="LocationTrackingService"/>.
///   - Persist every captured point through <see cref="DatabaseService"/>.
///   - Redraw the heat map on the Map control via <see cref="HeatMapRenderer"/>
///     whenever the saved points change.
/// </summary>
public partial class MainPage : ContentPage
{
    private readonly DatabaseService _databaseService;
    private readonly LocationTrackingService _trackingService;

    private readonly List<LocationPoint> _trackedPoints = new();

    public MainPage(DatabaseService databaseService, LocationTrackingService trackingService)
    {
        InitializeComponent();

        _databaseService = databaseService;
        _trackingService = trackingService;
        _trackingService.LocationCaptured += OnLocationCaptured;
    }

    /// <summary>
    /// Loads any previously saved points as soon as the page appears, so the
    /// heat map reflects the user's full history, not just the current
    /// session.
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var savedPoints = await _databaseService.GetAllLocationsAsync();
        _trackedPoints.Clear();
        _trackedPoints.AddRange(savedPoints);

        RedrawHeatMap();
        UpdateStatusLabel();

        if (_trackedPoints.Count > 0)
        {
            CenterMapOn(_trackedPoints[^1]);
        }
    }

    /// <summary>
    /// Toggles GPS tracking on/off and flips the button label/state to
    /// match. Shows an alert if the user denies the location permission.
    /// </summary>
    private async void OnStartStopClicked(object? sender, EventArgs e)
    {
        if (_trackingService.IsTracking)
        {
            _trackingService.Stop();
            StartStopButton.Text = "Start Tracking";
            return;
        }

        var started = await _trackingService.StartAsync();
        if (!started)
        {
            await DisplayAlertAsync(
                "Location permission needed",
                "Please allow location access so the app can track your position.",
                "OK");
            return;
        }

        StartStopButton.Text = "Stop Tracking";
    }

    /// <summary>
    /// Deletes every saved point from SQLite and clears the heat map.
    /// </summary>
    private async void OnClearClicked(object? sender, EventArgs e)
    {
        var confirmed = await DisplayAlertAsync(
            "Clear history",
            "This removes every saved location point. Continue?",
            "Yes",
            "No");

        if (!confirmed)
        {
            return;
        }

        await _databaseService.ClearAllLocationsAsync();
        _trackedPoints.Clear();

        RedrawHeatMap();
        UpdateStatusLabel();
    }

    /// <summary>
    /// Fired by <see cref="LocationTrackingService"/> every time a new GPS
    /// fix comes in. Saves it, adds it to the in-memory list, and redraws
    /// the map.
    /// </summary>
    private async void OnLocationCaptured(object? sender, LocationCapturedEventArgs e)
    {
        await _databaseService.SaveLocationAsync(e.Point);
        _trackedPoints.Add(e.Point);

        // UI updates must run on the main thread; GeolocationAccuracy
        // callbacks can arrive on a background thread.
        MainThread.BeginInvokeOnMainThread(() =>
        {
            RedrawHeatMap();
            UpdateStatusLabel();
            CenterMapOn(e.Point);
        });
    }

    private void RedrawHeatMap() => HeatMapRenderer.Render(LocationMap, _trackedPoints);

    private void UpdateStatusLabel() => StatusLabel.Text = $"{_trackedPoints.Count} points saved";

    private void CenterMapOn(LocationPoint point)
    {
        var center = new Location(point.Latitude, point.Longitude);
        LocationMap.MoveToRegion(MapSpan.FromCenterAndRadius(center, Distance.FromMeters(500)));
    }
}
