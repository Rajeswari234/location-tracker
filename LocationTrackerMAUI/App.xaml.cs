namespace LocationTrackerMAUI;

public partial class App : Application
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// App itself is constructed by the MAUI DI container (registered in
    /// MauiProgram), so it can ask for IServiceProvider directly in order
    /// to resolve MainPage and its own dependencies.
    /// </summary>
    public App(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Hosts MainPage inside a NavigationPage so the app gets a colored
    /// title bar at the top, matching the target layout.
    /// </summary>
    protected override Window CreateWindow(IActivationState? activationState)
    {
        var mainPage = _serviceProvider.GetRequiredService<MainPage>();

        var navigationPage = new NavigationPage(mainPage)
        {
            BarBackgroundColor = Color.FromArgb("#512BD4"),
            BarTextColor = Colors.White
        };

        return new Window(navigationPage);
    }
}
