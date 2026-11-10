using LocationTrackerMAUI.Models;
using SQLite;

namespace LocationTrackerMAUI.Services;

/// <summary>
/// Wraps all local SQLite access for the app. A single SQLite connection is
/// opened lazily and reused for the lifetime of the app, which is the
/// recommended pattern for sqlite-net-pcl in .NET MAUI.
/// </summary>
public class DatabaseService
{
    private const string DatabaseFileName = "locationtracker.db3";

    private SQLiteAsyncConnection? _connection;

    /// <summary>
    /// Opens the database connection (creating the file and the
    /// LocationPoints table the first time the app runs) and caches it for
    /// subsequent calls.
    /// </summary>
    private async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (_connection is not null)
        {
            return _connection;
        }

        var databasePath = Path.Combine(FileSystem.AppDataDirectory, DatabaseFileName);
        _connection = new SQLiteAsyncConnection(databasePath);
        await _connection.CreateTableAsync<LocationPoint>();

        return _connection;
    }

    /// <summary>
    /// Persists one captured GPS reading to the LocationPoints table.
    /// </summary>
    public async Task SaveLocationAsync(LocationPoint point)
    {
        var connection = await GetConnectionAsync();
        await connection.InsertAsync(point);
    }

    /// <summary>
    /// Returns every GPS reading saved so far, oldest first, so the map can
    /// redraw the full history (including the heat map) after the app
    /// restarts.
    /// </summary>
    public async Task<List<LocationPoint>> GetAllLocationsAsync()
    {
        var connection = await GetConnectionAsync();
        return await connection.Table<LocationPoint>()
                                .OrderBy(point => point.TimestampUtc)
                                .ToListAsync();
    }

    /// <summary>
    /// Deletes every saved location. Exposed so the user can reset their
    /// tracked history from the UI without reinstalling the app.
    /// </summary>
    public async Task ClearAllLocationsAsync()
    {
        var connection = await GetConnectionAsync();
        await connection.DeleteAllAsync<LocationPoint>();
    }
}
