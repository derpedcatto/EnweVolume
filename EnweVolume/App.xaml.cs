using CommunityToolkit.Mvvm.Messaging;
using EnweVolume.Core.Interfaces;
using EnweVolume.Core.Services;
using EnweVolume.MVVM.ViewModels;
using EnweVolume.MVVM.Views;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using System.Windows;

namespace EnweVolume;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public new static App Current => (App)Application.Current;
    public ServiceProvider Services { get; }

    public static string AppName { get; } = "EnweVolume";
    public static string SettingsFileName { get; } = "appsettings.json";
    public static string DefaultThemeName { get; } = "Default";
    public static List<CultureInfo> SupportedCultures { get; } =
    [
        new CultureInfo("en-US"),
        new CultureInfo("uk-UA"),
        new CultureInfo("ru-RU")
    ];

    public App()
    {
        Services = ConfigureServices();
        this.InitializeComponent();
    }

    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton<IMessenger, StrongReferenceMessenger>();
        services.AddSingleton<IShowToastNotificationService, ShowToastNotificationWindows>();
        services.AddSingleton<IUserSettingsService, UserSettingsService>();
        services.AddSingleton<IAudioMonitorService, AudioMonitorServiceWindows>();
        services.AddSingleton<ITrayIconManager, TrayIconManagerWindows>();

        services.AddTransient<SettingsWindow>();
        services.AddTransient<SettingsViewModel>();

        return services.BuildServiceProvider();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var settingsWindow = Services.GetRequiredService<SettingsWindow>();
        settingsWindow.Show();
    }
}