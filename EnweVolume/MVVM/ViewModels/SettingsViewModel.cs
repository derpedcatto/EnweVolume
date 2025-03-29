using System.Windows.Threading;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnweVolume.Core.Interfaces;
using CommunityToolkit.Mvvm.Messaging;
using System.Windows.Media;
using EnweVolume.Core.Models;

namespace EnweVolume.MVVM.ViewModels;

public partial class SettingsViewModel : ObservableObject, IDisposable
{
    private readonly int SAVE_DEBOUNCE_TIMER_INTERVAL = 1000;
    private readonly int AUDIO_MONITORING_POLLING_RATE = 50;
    private readonly int UI_UPDATE_TIMER_INTERVAL = 50;
    private readonly string DEFAULT_AUDIO_DEVICE_NAME = "Default";

    private readonly IMessenger _messenger;
    private readonly IShowToastNotificationService _showToastNotificationService;
    private readonly IAudioMonitorService _audioMonitorService;
    private readonly ITrayIconManager _trayIconManager;
    private readonly IUserSettingsService _userSettingsService;

    private UserSettings _userSettings;
    private DeviceSettings _currentDeviceSettings;
    private DispatcherTimer _uiUpdateTimer;
    private DispatcherTimer _saveDebounceTimer;
    private float _latestAudioLevel;
    private double _volumeBarWidth;

    public IRelayCommand<double> VolumeBarSizeChangedCommand { get; private set; }

    #region Observable Properties
    [ObservableProperty] private Brush _volumeBarColor;
    [ObservableProperty] private bool _changeBarColor;
    [ObservableProperty] private int _volumeCurrentValue;
    [ObservableProperty] private int _volumeBarRedThresholdLinePosition;
    [ObservableProperty] private int _volumeBarYellowThresholdLinePosition;
    [ObservableProperty] private int _volumeRedThreshold;
    [ObservableProperty] private bool _notificationRedPushEnabled;
    [ObservableProperty] private bool _notificationRedSoundEnabled;
    [ObservableProperty] private int _notificationRedSoundVolume;
    [ObservableProperty] private bool _thresholdYellowEnabled;
    [ObservableProperty] private int _volumeYellowThreshold;
    [ObservableProperty] private bool _notificationYellowPushEnabled;
    [ObservableProperty] private bool _notificationYellowSoundEnabled;
    [ObservableProperty] private int _notificationYellowSoundVolume;
    [ObservableProperty] private bool _startWithSystem;
    [ObservableProperty] private IEnumerable<string> _audioDeviceNamesList;
    [ObservableProperty] private string _audioDeviceSelected;
    [ObservableProperty] private IEnumerable<string> _localeList;
    [ObservableProperty] private string _localeSelected;
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

        InitializeTimers();
        InitializeAudioMonitoring();
    }

    public async Task Initialize()
    {
        await InitializeUserSettings();
    }

    private void InitializeAudioMonitoring()
    {
        _audioMonitorService.InitializeAudioMonitoring(AUDIO_MONITORING_POLLING_RATE);
        _audioMonitorService.VolumeLevelChanged += OnAudioLevelChanged;
        _audioMonitorService.DeviceListChanged += OnAudioDevicesChanged;
    }

    private void InitializeTimers()
    {
        _saveDebounceTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(SAVE_DEBOUNCE_TIMER_INTERVAL)
        };
        _saveDebounceTimer.Tick += async (s, e) =>
        {
            _saveDebounceTimer.Stop();
            await SaveUserSettings();
        };

        _uiUpdateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(UI_UPDATE_TIMER_INTERVAL)
        };
        _uiUpdateTimer.Tick += UpdateVolumeProgressBarUI;
        _uiUpdateTimer.Start();
    }

    private async Task InitializeUserSettings()
    {
        /* Fetching settings */
        var userSettingsResult = await _userSettingsService.GetSettings();
        if (userSettingsResult.IsSuccess && userSettingsResult.Value != null)
        {
            _userSettings = userSettingsResult.Value;
        }
        else
        {
            _userSettings = _userSettingsService.GetDefaultUserSettings();

            // TODO: Notify that settings have not been loaded
        }

        /* Applying settings */

        // User

        ChangeBarColor = _userSettings.ChangeProgressBarColorEnabled;
        StartWithSystem = _userSettings.StartWithSystemEnabled;

        // Locale

        LocaleList = App.SupportedCultures.Select(a => a.NativeName);

        var localeNameList = App.SupportedCultures.Select(a => a.Name);
        if (localeNameList.Contains(_userSettings.Locale))
        {
            var locale = App.SupportedCultures.FirstOrDefault(a => a.Name == _userSettings.Locale);
            LocaleSelected = locale.NativeName;
        }
        else
        {
            var defaultLocaleName = _userSettingsService.GetDefaultUserSettings().Locale;
            var locale = App.SupportedCultures.FirstOrDefault(a => a.Name == _userSettings.Locale);
            LocaleSelected = locale.NativeName;
        }

        OnLocaleSelectedChanged(LocaleSelected);

        // Devices

        AudioDeviceNamesList = new List<string> { DEFAULT_AUDIO_DEVICE_NAME }
            .Concat(_audioMonitorService.GetAllDeviceNames());

        string realDeviceName;
        if (_userSettings.CurrentDeviceName == DEFAULT_AUDIO_DEVICE_NAME)
        {
            realDeviceName = _audioMonitorService.GetCurrentDeviceName();
        }
        else
        {
            realDeviceName = _userSettings.CurrentDeviceName;
        }

        if (AudioDeviceNamesList.Contains(realDeviceName))
        {
            _audioMonitorService.SetDeviceByName(realDeviceName);
        }
        else
        {
            _audioMonitorService.SetDeviceDefault();
            _userSettings.CurrentDeviceName = DEFAULT_AUDIO_DEVICE_NAME;
        }

        if (_userSettings.DeviceProfiles.TryGetValue(realDeviceName, out var retrievedSettings))
        {
            _currentDeviceSettings = retrievedSettings;
        }
        else
        {
            _currentDeviceSettings = _userSettingsService.GetDefaultDeviceSettings(realDeviceName);
            _userSettings.DeviceProfiles[realDeviceName] = _currentDeviceSettings;
        }

        _userSettings.DeviceProfiles[realDeviceName] = _currentDeviceSettings;
        AudioDeviceSelected = _userSettings.CurrentDeviceName;

        await _userSettingsService.SaveSettings(_userSettings);
    }

    private void ResetSaveDebounceTimer()
    {
        _saveDebounceTimer.Stop();
        _saveDebounceTimer.Start();
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

    private void SaveCurrentDeviceSettings()
    {
        _userSettings.DeviceProfiles[_currentDeviceSettings.AudioDeviceName] = _currentDeviceSettings;
        ResetSaveDebounceTimer();
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

    private void OnAudioLevelChanged(float newLevel) => 
        _latestAudioLevel = newLevel;

    private void OnAudioDevicesChanged()
    {
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            var devices = new List<string> { DEFAULT_AUDIO_DEVICE_NAME }
                .Concat(_audioMonitorService.GetAllDeviceNames());
            AudioDeviceNamesList = devices;

            if (AudioDeviceSelected != DEFAULT_AUDIO_DEVICE_NAME && 
                !AudioDeviceNamesList.Contains(_audioMonitorService.GetCurrentDeviceName()))
            {
                AudioDeviceSelected = DEFAULT_AUDIO_DEVICE_NAME;
            }
        });
    }

    #region Partials

    partial void OnVolumeCurrentValueChanged(int oldValue, int newValue)
    {
        if (!ChangeBarColor)
        {
            VolumeBarColor = System.Windows.SystemColors.AccentColorBrush;
            return;
        }

        if (newValue <= VolumeRedThreshold)
        {
            if (ThresholdYellowEnabled && newValue >= VolumeYellowThreshold)
            {
                VolumeBarColor = Brushes.Yellow;
            }
            else
            {
                VolumeBarColor = Brushes.Green;
            }
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

        _currentDeviceSettings.VolumeRedThresholdValue = value;
        SaveCurrentDeviceSettings();
    }

    partial void OnVolumeYellowThresholdChanged(int value)
    {
        if (ThresholdYellowEnabled && value >= VolumeRedThreshold)
        {
            VolumeRedThreshold = value + 1;
        }
        UpdateThresholdLinePositions();

        _currentDeviceSettings.VolumeYellowThresholdValue = value;
        SaveCurrentDeviceSettings();
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

        _currentDeviceSettings.VolumeYellowThresholdEnabled = value;
        SaveCurrentDeviceSettings();
    }

    partial void OnNotificationRedPushEnabledChanged(bool oldValue, bool newValue)
    {
        _currentDeviceSettings.NotificationRedPushEnabled = newValue;
        SaveCurrentDeviceSettings();
    }

    partial void OnNotificationRedSoundEnabledChanged(bool oldValue, bool newValue)
    {
        _currentDeviceSettings.NotificationRedSoundEnabled = newValue;
        SaveCurrentDeviceSettings();
    }

    partial void OnNotificationRedSoundVolumeChanged(int oldValue, int newValue)
    {
        _currentDeviceSettings.NotificationRedSoundVolume = newValue;
        SaveCurrentDeviceSettings();
    }

    partial void OnNotificationYellowPushEnabledChanged(bool oldValue, bool newValue) 
    {
        _currentDeviceSettings.NotificationYellowPushEnabled = newValue;
        SaveCurrentDeviceSettings();
    }

    partial void OnNotificationYellowSoundEnabledChanged(bool oldValue, bool newValue)
    {
        _currentDeviceSettings.NotificationYellowSoundEnabled = newValue;
        SaveCurrentDeviceSettings();
    }

    partial void OnNotificationYellowSoundVolumeChanged(int oldValue, int newValue) 
    {
        _currentDeviceSettings.NotificationYellowSoundVolume = newValue;
        SaveCurrentDeviceSettings();
    }

    partial void OnAudioDeviceSelectedChanged(string value)
    {
        string realDeviceName;

        if (value == DEFAULT_AUDIO_DEVICE_NAME)
        {
            _audioMonitorService.SetDeviceDefault();
            realDeviceName = _audioMonitorService.GetCurrentDeviceName();
            _userSettings.CurrentDeviceName = DEFAULT_AUDIO_DEVICE_NAME;
        }
        else
        {
            _audioMonitorService.SetDeviceByName(value);
            realDeviceName = value;
            _userSettings.CurrentDeviceName = value;
        }

        // Load or create device profile
        if (!_userSettings.DeviceProfiles.TryGetValue(realDeviceName, out var settings))
        {
            settings = _userSettingsService.GetDefaultDeviceSettings(realDeviceName);
            _userSettings.DeviceProfiles[realDeviceName] = settings;
        }
        _currentDeviceSettings = settings;

        // Update all bound settings
        VolumeRedThreshold = _currentDeviceSettings.VolumeRedThresholdValue;
        VolumeYellowThreshold = _currentDeviceSettings.VolumeYellowThresholdValue;
        ThresholdYellowEnabled = _currentDeviceSettings.VolumeYellowThresholdEnabled;
        NotificationRedPushEnabled = _currentDeviceSettings.NotificationRedPushEnabled;
        NotificationRedSoundEnabled = _currentDeviceSettings.NotificationRedSoundEnabled;
        NotificationRedSoundVolume = _currentDeviceSettings.NotificationRedSoundVolume;
        NotificationYellowPushEnabled = _currentDeviceSettings.NotificationYellowPushEnabled;
        NotificationYellowSoundEnabled = _currentDeviceSettings.NotificationYellowSoundEnabled;
        NotificationYellowSoundVolume = _currentDeviceSettings.NotificationYellowSoundVolume;

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

    partial void OnStartWithSystemChanged(bool oldValue, bool newValue) 
    {
        _userSettings.StartWithSystemEnabled = newValue;
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
        _audioMonitorService.DeviceListChanged -= OnAudioDevicesChanged;

        _uiUpdateTimer?.Stop();

        _saveDebounceTimer.Stop();
        _ = SaveUserSettings().ConfigureAwait(false);
    }
}