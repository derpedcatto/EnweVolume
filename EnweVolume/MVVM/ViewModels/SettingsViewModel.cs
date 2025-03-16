using System.Windows.Threading;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NAudio.CoreAudioApi;
using EnweVolume.Core.Enums;
using EnweVolume.Core.Interfaces;
using CommunityToolkit.Mvvm.Messaging;
using System.Windows.Media;

namespace EnweVolume.MVVM.ViewModels;

public partial class SettingsViewModel : ObservableObject, IDisposable
{
    private readonly IMessenger _messenger;
    private readonly IShowToastNotificationService _showToastNotificationService;
    private readonly IAudioMonitorService _audioMonitorService;
    private readonly ITrayIconManager _trayIconManager;
    private readonly IUserSettingsService _userSettingsService;

    private DispatcherTimer _uiUpdateTimer;
    private float _latestAudioLevel;
    private bool _notificationRedThresholdSent;
    private bool _notificationYellowThresholdSent;

    // Current Volume
    [ObservableProperty]
    private Brush _volumeBarColor;

    [ObservableProperty]
    private bool _changeBarColor;

    [ObservableProperty]
    private int _volumeCurrentValue;

    // Red Threshold
    [ObservableProperty]
    private int _volumeRedThreshold;

    [ObservableProperty]
    private bool _notificationRedPushEnabled;

    [ObservableProperty]
    private bool _notificationRedSoundEnabled;

    [ObservableProperty]
    private int _notificationRedSoundVolume;

    // Yellow Threshold
    [ObservableProperty]
    private bool _thresholdYellowEnabled;

    [ObservableProperty]
    private int _volumeYellowThreshold;

    [ObservableProperty]
    private bool _notificationYellowPushEnabled;

    [ObservableProperty]
    private bool _notificationYellowSoundEnabled;

    [ObservableProperty]
    private int _notificationYellowSoundVolume;

    // General Settings
    [ObservableProperty]
    private bool _startWithSystem;

    [ObservableProperty]
    private IEnumerable<string> _audioDeviceNamesList;

    [ObservableProperty]
    private string _audioDeviceSelected;

    [ObservableProperty]
    private IEnumerable<string> _localeList;

    [ObservableProperty]
    private string _localeSelected;

    public SettingsViewModel(
        IMessenger messenger, 
        IAudioMonitorService audioMonitorService,
        IShowToastNotificationService showToastNotificationService,
        ITrayIconManager trayIconManager,
        IUserSettingsService userSettingsService)
    {
        _messenger = messenger;
        _showToastNotificationService = showToastNotificationService;
        _audioMonitorService = audioMonitorService;
        _trayIconManager = trayIconManager;
        _userSettingsService = userSettingsService;
    }

    public async Task Initialize()
    {
        // temp values
        VolumeYellowThreshold = 50;
        VolumeRedThreshold = 75;
        NotificationRedSoundVolume = 20;
        NotificationYellowSoundVolume = 20;
        LocaleList = App.SupportedCultures.ToList().Select(x => x.NativeName);
        LocaleSelected = LocaleList.FirstOrDefault()!;
        AudioDeviceNamesList = _audioMonitorService.GetAllDeviceNames();
        AudioDeviceSelected = AudioDeviceNamesList.FirstOrDefault()!;
        //

        _audioMonitorService.InitializeAudioMonitoring(string.Empty, 75);
        _audioMonitorService.VolumeLevelChanged += OnAudioLevelChanged;

        _uiUpdateTimer = new DispatcherTimer()
        {
            Interval = TimeSpan.FromMilliseconds(120)
        };
        _uiUpdateTimer.Tick += UpdateVolumeProgressBarUI;
        _uiUpdateTimer.Start();
    }

    private void OnAudioLevelChanged(float newLevel)
    {
        _latestAudioLevel = newLevel;
    }

    private void UpdateVolumeProgressBarUI(object sender, EventArgs e)
    {
        VolumeCurrentValue = (int)(_latestAudioLevel * 100);
    }

    partial void OnVolumeCurrentValueChanged(int oldValue, int newValue)
    {
        if (!ChangeBarColor)
        {
            // TODO: Non-windows approach
            VolumeBarColor = System.Windows.SystemColors.AccentColorBrush;
            return;
        }

        if (newValue <= VolumeYellowThreshold)
        {
            VolumeBarColor = Brushes.Green;
        }
        else if (newValue <= VolumeRedThreshold)
        {
            VolumeBarColor = Brushes.Yellow;
        }
        else
        {
            VolumeBarColor = Brushes.Red;
        }
    }

    public void Dispose()
    {
        _audioMonitorService.VolumeLevelChanged -= OnAudioLevelChanged;
        _uiUpdateTimer?.Stop();
    }
}