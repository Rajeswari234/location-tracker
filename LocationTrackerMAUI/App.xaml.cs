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

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var mainPage = _serviceProvider.GetRequiredService<MainPage>();
        return new Window(mainPage);
    }
}
