using CommunityToolkit.Mvvm.Messaging;
using EnweVolume.Core.Enums;
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
    public Dictionary<VolumeLevel, Uri> DefaultIconSet { get; } = new Dictionary<VolumeLevel, Uri>
    {
        { VolumeLevel.Green, new Uri("Resources/TrayIcons/green.ico", UriKind.Relative) },
        { VolumeLevel.Yellow, new Uri("Resources/TrayIcons/yellow.ico", UriKind.Relative) },
        { VolumeLevel.Red, new Uri("Resources/TrayIcons/red.ico", UriKind.Relative) }
    };

    public static string AppName { get; } = "EnweVolume";
    public static string SettingsFileName { get; } = "appsettings.json";
    public static string DefaultThemeName { get; } = "Default";
    public static List<CultureInfo> SupportedCultures { get; } =
    [
        new CultureInfo("en-US"),
        new CultureInfo("en-GB"),
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
        services.AddSingleton<ITrayManager, TrayManager>();
        services.AddSingleton<IViewVisibilityService, ViewVisibilityService>();

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

    public static void ApplyCulture(CultureInfo culture)
    {
        var mergedDict = Current.Resources.MergedDictionaries;
        string resourcePath = $"Resources/StringResources.{culture.Name}.xaml";

        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;

        var oldDict = mergedDict
            .Where(d => d.Source?.OriginalString.StartsWith("Resources/StringResources.") == true)
            .ToList();

        foreach (var dict in oldDict)
        {
            mergedDict.Remove(dict);
        }

        var newDict = new ResourceDictionary { Source = new Uri(resourcePath, UriKind.Relative) };
        mergedDict.Add(newDict);
    }

    public static string GetString(string key)
    {
        if (Current.TryFindResource(key) is string resourceValue)
        {
            return resourceValue;
        }

        return string.Empty;
    }
}