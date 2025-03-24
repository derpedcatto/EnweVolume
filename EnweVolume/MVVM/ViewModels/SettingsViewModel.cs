using System.Windows.Threading;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NAudio.CoreAudioApi;
using EnweVolume.Core.Enums;
using EnweVolume.Core.Interfaces;
using CommunityToolkit.Mvvm.Messaging;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Shapes;
using EnweVolume.Core.Models;

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
    private double _volumeBarWidth;
    private bool _notificationRedThresholdSent;
    private bool _notificationYellowThresholdSent;
    private UserSettings _userSettings;

    public IRelayCommand<double> VolumeBarSizeChangedCommand { get; private set; }

    #region Observable Properties

    // Current Volume
    [ObservableProperty]
    private Brush _volumeBarColor;

    [ObservableProperty]
    private bool _changeBarColor;

    [ObservableProperty]
    private int _volumeCurrentValue;

    [ObservableProperty]
    private int _volumeBarRedThresholdLinePosition;

    [ObservableProperty]
    private int _volumeBarYellowThresholdLinePosition;

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

    #endregion

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

        VolumeBarSizeChangedCommand = new RelayCommand<double>(OnVolumeBarSizeChanged);
    }

    public async Task Initialize()
    {
        await GetUserSettings();
        SetUserSettings();

        _audioMonitorService.InitializeAudioMonitoring(50);
        _audioMonitorService.VolumeLevelChanged += OnAudioLevelChanged;

        _uiUpdateTimer = new DispatcherTimer()
        {
            Interval = TimeSpan.FromMilliseconds(50)
        };
        _uiUpdateTimer.Tick += UpdateVolumeProgressBarUI;
        _uiUpdateTimer.Start();

        UpdateThresholdLinePositions();
    }

    private async Task GetUserSettings()
    {
        var userSettingsResult = await _userSettingsService.GetSettings();
        if (userSettingsResult.IsSuccess)
        {
            _userSettings = userSettingsResult.Value;
        }
        else
        {
            // TODO: Notify that settings have not been loaded
            // Loading and saving default settings
            _userSettings = _userSettingsService.GetDefaultSettings();
            await _userSettingsService.SaveSettings(_userSettings);
        }
    }

    private void SetUserSettings()
    {
        VolumeRedThreshold = _userSettings.VolumeRedThresholdValue;
        VolumeYellowThreshold = _userSettings.VolumeYellowThresholdValue;
        NotificationRedPushEnabled = _userSettings.NotificationRedPushEnabled;
        NotificationYellowPushEnabled = _userSettings.NotificationYellowPushEnabled;
        NotificationRedSoundEnabled = _userSettings.NotificationRedSoundEnabled;
        NotificationYellowSoundEnabled = _userSettings.NotificationYellowSoundEnabled;
        NotificationRedSoundVolume = _userSettings.NotificationRedSoundVolume;
        NotificationYellowSoundVolume = _userSettings.NotificationYellowSoundVolume;
        ChangeBarColor = _userSettings.ChangeProgressBarColorEnabled;
        StartWithSystem = _userSettings.StartWithSystemEnabled;

        AudioDeviceNamesList = _audioMonitorService.GetAllDeviceNames();

        if (_userSettings.AudioDeviceName != string.Empty &&
            AudioDeviceNamesList.Contains(_userSettings.AudioDeviceName))
        {
            _audioMonitorService.SetDeviceByName(_userSettings.AudioDeviceName);
        }
        else
        {
            _audioMonitorService.SetDeviceDefault();
        }

        LocaleList = App.SupportedCultures.Select(a => a.NativeName);

        var localeNameList = App.SupportedCultures.Select(a => a.Name);
        if (localeNameList.Contains(_userSettings.Locale))
        {
            var locale = App.SupportedCultures.FirstOrDefault(a => a.Name == _userSettings.Locale);
            LocaleSelected = LocaleList.FirstOrDefault(locale.NativeName);
        }
        else
        {
            var defaultLocaleName = _userSettingsService.GetDefaultSettings().Locale;
            var locale = App.SupportedCultures.FirstOrDefault(a => a.Name == _userSettings.Locale);
            LocaleSelected = locale.NativeName;
        }

        OnLocaleSelectedChanged(LocaleSelected);
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
            if (ThresholdYellowEnabled)
                VolumeBarColor = Brushes.Yellow;
            else
                VolumeBarColor = Brushes.Green;
        }
        else
        {
            VolumeBarColor = Brushes.Red;
        }
    }

    private void OnVolumeBarSizeChanged(double volumeBarWidth)
    {
        _volumeBarWidth = volumeBarWidth;
        UpdateThresholdLinePositions();
    }

    private void UpdateThresholdLinePositions()
    {
        VolumeBarRedThresholdLinePosition = (int)(VolumeRedThreshold * _volumeBarWidth / 100);
        VolumeBarYellowThresholdLinePosition = (int)(VolumeYellowThreshold * _volumeBarWidth / 100);
    }

    partial void OnVolumeRedThresholdChanged(int value)
    {
        if (ThresholdYellowEnabled && value <= VolumeYellowThreshold)
        {
            VolumeYellowThreshold = value - 1;        
        }

        UpdateThresholdLinePositions();
    }

    partial void OnVolumeYellowThresholdChanged(int value)
    {
        if (ThresholdYellowEnabled && value >= VolumeRedThreshold)
        {
            VolumeRedThreshold = value + 1;
        }

        UpdateThresholdLinePositions();
    }

    partial void OnThresholdYellowEnabledChanged(bool value)
    {
        if (value)
        {
            if (VolumeRedThreshold <= VolumeYellowThreshold)
            {
                VolumeYellowThreshold = VolumeRedThreshold - 1;
            }
        }

        UpdateThresholdLinePositions();
    }

    partial void OnLocaleSelectedChanged(string value)
    {
        var selectedCulture = App.SupportedCultures.FirstOrDefault(c => c.NativeName == value);
        if (selectedCulture != null)
        {
            App.ApplyCulture(selectedCulture);
        }
    }

    public void Dispose()
    {
        _audioMonitorService.VolumeLevelChanged -= OnAudioLevelChanged;
        _uiUpdateTimer?.Stop();
    }
}