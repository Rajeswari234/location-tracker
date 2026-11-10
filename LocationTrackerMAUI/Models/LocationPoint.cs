using SQLite;

namespace LocationTrackerMAUI.Models;

/// <summary>
/// Represents a single GPS reading captured while the app is tracking the
/// user's movement. Each instance maps directly to one row in the local
/// SQLite "LocationPoints" table.
/// </summary>
public class LocationPoint
{
    /// <summary>
    /// Primary key, auto-incremented by SQLite.
    /// </summary>
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>
    /// Latitude of the captured GPS reading, in decimal degrees.
    /// </summary>
    public double Latitude { get; set; }

    /// <summary>
    /// Longitude of the captured GPS reading, in decimal degrees.
    /// </summary>
    public double Longitude { get; set; }

    /// <summary>
    /// UTC timestamp of when this point was recorded. Stored so that the
    /// user's path can be replayed chronologically if needed.
    /// </summary>
    public DateTime TimestampUtc { get; set; }
}
