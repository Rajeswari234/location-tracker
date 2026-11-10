using LocationTrackerMAUI.Models;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;

// Disambiguate from Microsoft.Maui.ApplicationModel.Map (the "open in maps app" API).
using Map = Microsoft.Maui.Controls.Maps.Map;

namespace LocationTrackerMAUI.Services;

/// <summary>
/// .NET MAUI's <see cref="Map"/> control has no built-in heat map layer, so
/// this class extends it by turning a list of raw <see cref="LocationPoint"/>
/// readings into a set of semi-transparent <see cref="Circle"/> map elements.
///
/// The approach:
///   1. Snap every point onto a small latitude/longitude grid so that GPS
///      readings taken close together are grouped into one "cell".
///   2. Count how many readings fall into each cell - that count is the
///      cell's "heat".
///   3. Draw one circle per cell, sized and colored by that heat, from cool
///      (sparse) to hot (dense), the same visual language as a classic heat
///      map.
/// </summary>
public static class HeatMapRenderer
{
    /// <summary>
    /// Grid resolution in degrees. ~0.0007 degrees of latitude is roughly
    /// 75 meters, which groups readings from the same stop/area together
    /// without merging an entire city block into one cell.
    /// </summary>
    private const double GridResolutionDegrees = 0.0007;

    /// <summary>Base radius, in meters, for a cell visited only once.</summary>
    private const double BaseRadiusMeters = 12;

    /// <summary>Extra radius, in meters, added per additional visit (capped).</summary>
    private const double RadiusPerVisitMeters = 6;

    private const int MaxVisitsForScaling = 10;

    /// <summary>
    /// Clears any previously drawn heat map circles from <paramref name="map"/>
    /// and redraws them from the current <paramref name="points"/>.
    /// </summary>
    public static void Render(Map map, IReadOnlyCollection<LocationPoint> points)
    {
        ArgumentNullException.ThrowIfNull(map);

        map.MapElements.Clear();

        if (points.Count == 0)
        {
            return;
        }

        foreach (var cell in BuildHeatMapCells(points))
        {
            map.MapElements.Add(BuildCircle(cell));
        }
    }

    /// <summary>
    /// Groups raw GPS points onto a coarse grid and counts how many points
    /// fall into each grid cell.
    /// </summary>
    private static IEnumerable<HeatMapCell> BuildHeatMapCells(IReadOnlyCollection<LocationPoint> points)
    {
        return points
            .GroupBy(point => (
                LatBucket: Math.Round(point.Latitude / GridResolutionDegrees),
                LonBucket: Math.Round(point.Longitude / GridResolutionDegrees)))
            .Select(group => new HeatMapCell
            {
                // Use the bucket's center, not an individual reading, so
                // the circle represents the whole group of points.
                Latitude = group.Key.LatBucket * GridResolutionDegrees,
                Longitude = group.Key.LonBucket * GridResolutionDegrees,
                VisitCount = group.Count()
            });
    }

    /// <summary>
    /// Converts one heat map cell into a drawable, color- and size-scaled
    /// map <see cref="Circle"/>.
    /// </summary>
    private static Circle BuildCircle(HeatMapCell cell)
    {
        var cappedVisits = Math.Min(cell.VisitCount, MaxVisitsForScaling);
        var radius = BaseRadiusMeters + (cappedVisits - 1) * RadiusPerVisitMeters;
        var color = GetHeatColor(cappedVisits);

        return new Circle
        {
            Center = new Location(cell.Latitude, cell.Longitude),
            Radius = Distance.FromMeters(radius),
            StrokeColor = color,
            StrokeWidth = 1,
            FillColor = color.WithAlpha(0.35f)
        };
    }

    /// <summary>
    /// Maps a visit count to a color on a cool-to-hot gradient:
    /// a single visit is blue/green ("cool"), and the busiest cells shade
    /// through yellow into red ("hot") - the standard heat map palette.
    /// </summary>
    private static Color GetHeatColor(int visitCount)
    {
        // Normalize to 0..1 across the capped visit range.
        var intensity = (visitCount - 1) / (double)(MaxVisitsForScaling - 1);
        intensity = Math.Clamp(intensity, 0, 1);

        return intensity switch
        {
            < 0.25 => Colors.DeepSkyBlue,
            < 0.5 => Colors.LimeGreen,
            < 0.75 => Colors.Orange,
            _ => Colors.Red
        };
    }
}
