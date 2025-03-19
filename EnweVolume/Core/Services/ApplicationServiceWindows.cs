using CommunityToolkit.Mvvm.Messaging;
using EnweVolume.Core.Interfaces;
using EnweVolume.Core.Messages;
using EnweVolume.MVVM.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using System.Windows;

namespace EnweVolume.Core.Services;

public class ApplicationServiceWindows : IApplicationService, IDisposable
{
    private SettingsWindow _settingsWindow;
    private readonly ITrayIconManager _trayIconManager;
    private readonly IAudioMonitorService _audioMonitorService;
    private readonly IUserSettingsService _userSettingsService;
    private readonly IMessenger _messenger;

    public ApplicationServiceWindows(
        ITrayIconManager trayIconManager,
        IAudioMonitorService audioMonitorService,
        IUserSettingsService userSettingsService,
        IMessenger messenger)
    {
        _trayIconManager = trayIconManager;
        _audioMonitorService = audioMonitorService;
        _userSettingsService = userSettingsService;
        _messenger = messenger;

        _messenger.Register<MinimizeToTrayMessage>(this, (r, m) => HideToTray());
        _messenger.Register<ExitApplicationMessage>(this, (r, m) => Exit());
    }

    public void Initialize()
    {
        _settingsWindow = App.Current.Services.GetRequiredService<SettingsWindow>();

        // init tray
        // init audio monitor

        SystemEvents.SessionEnding += OnSessionEnding;
    }

    public void HideToTray()
    {
        _settingsWindow.Hide();
    }

    public void ShowSettingsWindow()
    {
        _settingsWindow.Show();
        _settingsWindow.WindowState = WindowState.Normal;
        _settingsWindow.Activate();
    }

    public void Exit()
    {
        SystemEvents.SessionEnding -= OnSessionEnding;

        // Dispose

        Application.Current.Shutdown();
    }

    private void OnSessionEnding(object sender, SessionEndingEventArgs e)
    {
        Exit();
    }

    public void Dispose()
    {
        _messenger.UnregisterAll(this);
        // dispose audio monitor
        // dispose tray manager?

        if (_settingsWindow != null)
        {
            // _settingsWindow.Closing -= SettingsWindow_Closing;
            // _settingsWindow.StateChanged -= SettingsWindow_StateChanged;
        }
    }
}
