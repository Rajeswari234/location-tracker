using LocationTrackerMAUI.Services;
using Microsoft.Extensions.Logging;

namespace LocationTrackerMAUI;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiMaps(); // Registers the .NET MAUI Maps control/handlers.
            // Add custom fonts here with .ConfigureFonts(...) if any are
            // placed under Resources/Fonts.

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Services are registered as singletons: one SQLite connection and
        // one tracking/timer instance should live for the whole app session.
        builder.Services.AddSingleton<DatabaseService>();
        builder.Services.AddSingleton<LocationTrackingService>();
        builder.Services.AddSingleton<MainPage>();

        return builder.Build();
    }
}
