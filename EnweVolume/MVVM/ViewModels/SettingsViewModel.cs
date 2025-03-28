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
    private readonly int SAVE_DEVOUNCE_TIMER_INTERVAL = 1000;
    private readonly int AUDIO_MONITORING_POLLING_RATE = 50;
    private readonly int UI_UPDATE_TIMER_INTERVAL = 50;

    private readonly IMessenger _messenger;
    private readonly IShowToastNotificationService _showToastNotificationService;
    private readonly IAudioMonitorService _audioMonitorService;
    private readonly ITrayIconManager _trayIconManager;
    private readonly IUserSettingsService _userSettingsService;

    private UserSettings _userSettings;
    private DispatcherTimer _uiUpdateTimer;
    private DispatcherTimer _saveDebounceTimer;
    private float _latestAudioLevel;
    private double _volumeBarWidth;

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

        _saveDebounceTimer = new DispatcherTimer()
        {
            Interval = TimeSpan.FromMilliseconds(SAVE_DEVOUNCE_TIMER_INTERVAL),
        };
        _saveDebounceTimer.Tick += async (s, e) =>
        {
            _saveDebounceTimer.Stop();
            await SaveUserSettings();
        };

        _audioMonitorService.InitializeAudioMonitoring(AUDIO_MONITORING_POLLING_RATE);
        _audioMonitorService.VolumeLevelChanged += OnAudioLevelChanged;

        _uiUpdateTimer = new DispatcherTimer()
        {
            Interval = TimeSpan.FromMilliseconds(UI_UPDATE_TIMER_INTERVAL)
        };
        _uiUpdateTimer.Tick += UpdateVolumeProgressBarUI;
        _uiUpdateTimer.Start();

        UpdateThresholdLinePositions();
    }

    public async Task Initialize()
    {
        await GetUserSettings();
        SetUserSettings();
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

    private async Task SaveUserSettings()
    {
        try
        {
            var result = await _userSettingsService.SaveSettings(_userSettings);
            if (!result.IsSuccess)
            {
                // _showToastNotificationService.ShowError("Failed to save settings.");
            }
        }
        catch (Exception ex)
        {
            // _showToastNotificationService.ShowError($"Error saving settings: {ex.Message}");
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
            LocaleSelected = locale.NativeName;
        }
        else
        {
            var defaultLocaleName = _userSettingsService.GetDefaultSettings().Locale;
            var locale = App.SupportedCultures.FirstOrDefault(a => a.Name == _userSettings.Locale);
            LocaleSelected = locale.NativeName;
        }

        OnLocaleSelectedChanged(LocaleSelected);
    }

    private void UpdateVolumeProgressBarUI(object sender, EventArgs e)
    {
        VolumeCurrentValue = (int)(_latestAudioLevel * 100);
    }

    private void UpdateThresholdLinePositions()
    {
        VolumeBarRedThresholdLinePosition = (int)(VolumeRedThreshold * _volumeBarWidth / 100);
        VolumeBarYellowThresholdLinePosition = (int)(VolumeYellowThreshold * _volumeBarWidth / 100);
    }

    private void OnVolumeBarSizeChanged(double volumeBarWidth)
    {
        _volumeBarWidth = volumeBarWidth;
        UpdateThresholdLinePositions();
    }

    private void OnAudioLevelChanged(float newLevel) => _latestAudioLevel = newLevel;

    private void ResetSaveDebounceTimer()
    {
        _saveDebounceTimer.Stop();
        _saveDebounceTimer.Start();
    }

    #region Partials

    partial void OnVolumeCurrentValueChanged(int oldValue, int newValue)
    {
        if (!ChangeBarColor)
        {
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

    partial void OnVolumeRedThresholdChanged(int value)
    {
        if (ThresholdYellowEnabled && value <= VolumeYellowThreshold)
        {
            VolumeYellowThreshold = value - 1;        
        }
        UpdateThresholdLinePositions();

        _userSettings.VolumeRedThresholdValue = value;
        ResetSaveDebounceTimer();
    }

    partial void OnVolumeYellowThresholdChanged(int value)
    {
        if (ThresholdYellowEnabled && value >= VolumeRedThreshold)
        {
            VolumeRedThreshold = value + 1;
        }
        UpdateThresholdLinePositions();

        _userSettings.VolumeYellowThresholdValue = value;
        ResetSaveDebounceTimer();
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

        _userSettings.VolumeYellowThresholdEnabled = value;
        ResetSaveDebounceTimer();
    }

    partial void OnLocaleSelectedChanged(string value)
    {
        var selectedCulture = App.SupportedCultures.FirstOrDefault(c => c.NativeName == value);
        if (selectedCulture != null)
        {
            App.ApplyCulture(selectedCulture);
        }

        _userSettings.Locale = selectedCulture.Name;
        ResetSaveDebounceTimer();
    }

    partial void OnNotificationRedPushEnabledChanged(bool oldValue, bool newValue)
    {
        _userSettings.NotificationRedPushEnabled = newValue;
        ResetSaveDebounceTimer();
    }

    partial void OnNotificationRedSoundEnabledChanged(bool oldValue, bool newValue)
    {
        _userSettings.NotificationRedSoundEnabled = newValue;
        ResetSaveDebounceTimer();
    }

    partial void OnNotificationRedSoundVolumeChanged(int oldValue, int newValue)
    {
        _userSettings.NotificationRedSoundVolume = newValue;
        ResetSaveDebounceTimer();
    }

    partial void OnNotificationYellowPushEnabledChanged(bool oldValue, bool newValue) 
    {
        _userSettings.NotificationYellowPushEnabled = newValue;
        ResetSaveDebounceTimer();
    }

    partial void OnNotificationYellowSoundEnabledChanged(bool oldValue, bool newValue)
    {
        _userSettings.NotificationYellowSoundEnabled = newValue;
        ResetSaveDebounceTimer();
    }

    partial void OnNotificationYellowSoundVolumeChanged(int oldValue, int newValue) 
    {
        _userSettings.NotificationYellowSoundVolume = newValue;
        ResetSaveDebounceTimer();
    }

    partial void OnStartWithSystemChanged(bool oldValue, bool newValue) 
    {
        _userSettings.StartWithSystemEnabled = newValue;
        ResetSaveDebounceTimer();
    }

    partial void OnAudioDeviceSelectedChanged(string oldValue, string newValue) 
    {
        // TODO: AudioService change device
        _userSettings.AudioDeviceName = newValue;
        ResetSaveDebounceTimer();
    }

    partial void OnChangeBarColorChanged(bool oldValue, bool newValue)
    {
        _userSettings.ChangeProgressBarColorEnabled = newValue;
        ResetSaveDebounceTimer();
    }

    #endregion

    public void Dispose()
    {
        _audioMonitorService.VolumeLevelChanged -= OnAudioLevelChanged;
        _uiUpdateTimer?.Stop();

        _saveDebounceTimer.Stop();
        _ = SaveUserSettings().ConfigureAwait(false);
    }
}