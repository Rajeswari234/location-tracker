namespace LocationTrackerMAUI.Models;

/// <summary>
/// Represents one grid cell of the heat map. Multiple raw GPS points that
/// fall within the same small geographic area are grouped into a single
/// cell so that the map can show "how many times" the user visited that
/// area, rather than drawing one overlapping circle per reading.
/// </summary>
public class HeatMapCell
{
    /// <summary>Latitude of the cell's center point.</summary>
    public double Latitude { get; set; }

    /// <summary>Longitude of the cell's center point.</summary>
    public double Longitude { get; set; }

    /// <summary>
    /// Number of raw <see cref="LocationPoint"/> readings that fall inside
    /// this cell. Higher counts mean the user spent more time in this spot,
    /// which is what drives the "heat" (color/opacity) of the cell.
    /// </summary>
    public int VisitCount { get; set; }
}
