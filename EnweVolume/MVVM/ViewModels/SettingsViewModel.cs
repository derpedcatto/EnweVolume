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
    [ObservableProperty] private Brush _volumeBarBrush;
    [ObservableProperty] private bool _progressBarColorChangeEnabled;
    [ObservableProperty] private int _currentVolume;
    [ObservableProperty] private int _redThresholdLinePosition;
    [ObservableProperty] private int _yellowThresholdLinePosition;
    [ObservableProperty] private int _redThresholdVolume;
    [ObservableProperty] private bool _redPushNotificationEnabled;
    [ObservableProperty] private bool _redSoundNotificationEnabled;
    [ObservableProperty] private int _redSoundNotificationVolume;
    [ObservableProperty] private bool _yellowThresholdEnabled;
    [ObservableProperty] private int _yellowThresholdVolume;
    [ObservableProperty] private bool _yellowPushNotificationEnabled;
    [ObservableProperty] private bool _yellowSoundNotificationEnabled;
    [ObservableProperty] private int _yellowSoundNotificationVolume;
    [ObservableProperty] private bool _launchOnStartup;
    [ObservableProperty] private IEnumerable<string> _audioDeviceNames;
    [ObservableProperty] private string _selectedAudioDevice;
    [ObservableProperty] private IEnumerable<string> _localeList;
    [ObservableProperty] private string _selectedLocale;
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

        VolumeBarSizeChangedCommand = new RelayCommand<double>(OnProgressBarSizeChanged);

        InitializeTimers();
        InitializeAudioMonitoring();
    }

    #region Initializers

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

        ProgressBarColorChangeEnabled = _userSettings.IsProgressBarColorChangeEnabled;
        LaunchOnStartup = _userSettings.LaunchOnStartup;

        // Locale

        LocaleList = App.SupportedCultures.Select(a => a.NativeName);

        var localeNameList = App.SupportedCultures.Select(a => a.Name);
        if (localeNameList.Contains(_userSettings.SelectedLocale))
        {
            var locale = App.SupportedCultures.FirstOrDefault(a => a.Name == _userSettings.SelectedLocale);
            SelectedLocale = locale.NativeName;
        }
        else
        {
            var defaultLocaleName = _userSettingsService.GetDefaultUserSettings().SelectedLocale;
            var locale = App.SupportedCultures.FirstOrDefault(a => a.Name == _userSettings.SelectedLocale);
            SelectedLocale = locale.NativeName;
        }

        OnSelectedLocaleChanged(SelectedLocale);

        // Devices
        // TODO: New Audio Monitoring
        // UpdateBindedValues(_userSettings.DeviceProfiles[CurrentDeviceName])
        
        await _userSettingsService.SaveSettings(_userSettings);
    }

    #endregion

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
        catch (Exception)
        {
            // _showToastNotificationService.ShowError($"Error saving settings: {ex.Message}");
        }
    }

    private void SaveCurrentDeviceSettings()
    {
        // TODO: New Audio Monitoring
        UpdateBindedValues();
        ResetSaveDebounceTimer();
    }

    private void ResetSaveDebounceTimer()
    {
        _saveDebounceTimer.Stop();
        _saveDebounceTimer.Start();
    }

    private void OnAudioLevelChanged(float newLevel) => _latestAudioLevel = newLevel;

    private void OnAudioDevicesChanged()
    {
        // TODO: New Audio Monitoring
    }

    #region UI

    private void UpdateBindedValues()
    {
        RedThresholdVolume = _currentDeviceSettings.RedThresholdVolume;
        YellowThresholdVolume = _currentDeviceSettings.YellowThresholdVolume;
        YellowThresholdEnabled = _currentDeviceSettings.IsYellowThresholdEnabled;
        RedPushNotificationEnabled = _currentDeviceSettings.IsRedPushNotificationEnabled;
        RedSoundNotificationEnabled = _currentDeviceSettings.IsRedSoundNotificationEnabled;
        RedSoundNotificationVolume = _currentDeviceSettings.RedSoundNotificationVolume;
        YellowPushNotificationEnabled = _currentDeviceSettings.IsYellowPushNotificationEnabled;
        YellowSoundNotificationEnabled = _currentDeviceSettings.IsYellowSoundNotificationEnabled;
        YellowSoundNotificationVolume = _currentDeviceSettings.YellowSoundNotificationVolume;
    }

    private void UpdateVolumeProgressBarUI(object sender, EventArgs e)
    {
        CurrentVolume = (int)(_latestAudioLevel * 100);
    }

    private void UpdateThresholdLinePositions()
    {
        RedThresholdLinePosition = (int)(RedThresholdVolume * _volumeBarWidth / 100);
        YellowThresholdLinePosition = (int)(YellowThresholdVolume * _volumeBarWidth / 100);
    }

    private void OnProgressBarSizeChanged(double volumeBarWidth)
    {
        _volumeBarWidth = volumeBarWidth;
        UpdateThresholdLinePositions();
    }

    #endregion

    #region Property Changed Handlers

    partial void OnCurrentVolumeChanged(int value)
    {
        if (!ProgressBarColorChangeEnabled)
        {
            VolumeBarBrush = System.Windows.SystemColors.AccentColorBrush;
            return;
        }

        if (value <= RedThresholdVolume)
        {
            if (YellowThresholdEnabled && value >= YellowThresholdVolume)
            {
                VolumeBarBrush = Brushes.Yellow;
            }
            else
            {
                VolumeBarBrush = Brushes.Green;
            }
        }
        else
        {
            VolumeBarBrush = Brushes.Red;
        }
    }

    partial void OnRedThresholdVolumeChanged(int value)
    {
        if (YellowThresholdEnabled && value <= YellowThresholdVolume)
        {
            YellowThresholdVolume = value - 1;        
        }
        UpdateThresholdLinePositions();

        _currentDeviceSettings.RedThresholdVolume = value;
        SaveCurrentDeviceSettings();
    }

    partial void OnYellowThresholdVolumeChanged(int value)
    {
        if (YellowThresholdEnabled && value >= RedThresholdVolume)
        {
            RedThresholdVolume = value + 1;
        }
        UpdateThresholdLinePositions();

        _currentDeviceSettings.YellowThresholdVolume = value;
        SaveCurrentDeviceSettings();
    }

    partial void OnYellowThresholdEnabledChanged(bool value)
    {
        if (value)
        {
            if (RedThresholdVolume <= YellowThresholdVolume)
            {
                YellowThresholdVolume = RedThresholdVolume - 1;
            }
        }
        UpdateThresholdLinePositions();

        _currentDeviceSettings.IsYellowThresholdEnabled = value;
        SaveCurrentDeviceSettings();
    }

    partial void OnRedPushNotificationEnabledChanged(bool value)
    {
        _currentDeviceSettings.IsRedPushNotificationEnabled = value;
        SaveCurrentDeviceSettings();
    }

    partial void OnRedSoundNotificationEnabledChanged(bool value)
    {
        _currentDeviceSettings.IsRedSoundNotificationEnabled = value;
        SaveCurrentDeviceSettings();
    }

    partial void OnRedSoundNotificationVolumeChanged(int value)
    {
        _currentDeviceSettings.RedSoundNotificationVolume = value;
        SaveCurrentDeviceSettings();
    }

    partial void OnYellowPushNotificationEnabledChanged(bool value) 
    {
        _currentDeviceSettings.IsYellowPushNotificationEnabled = value;
        SaveCurrentDeviceSettings();
    }

    partial void OnYellowSoundNotificationEnabledChanged(bool value)
    {
        _currentDeviceSettings.IsYellowSoundNotificationEnabled = value;
        SaveCurrentDeviceSettings();
    }

    partial void OnYellowSoundNotificationVolumeChanged(int value) 
    {
        _currentDeviceSettings.YellowSoundNotificationVolume = value;
        SaveCurrentDeviceSettings();
    }

    partial void OnSelectedAudioDeviceChanged(string value)
    {
        // TODO: New Audio Monitoring

        UpdateBindedValues();
        ResetSaveDebounceTimer();
    }

    partial void OnSelectedLocaleChanged(string value)
    {
        var selectedCulture = App.SupportedCultures.FirstOrDefault(c => c.NativeName == value);
        if (selectedCulture != null)
        {
            App.ApplyCulture(selectedCulture);
        }

        _userSettings.SelectedLocale = selectedCulture.Name;

        ResetSaveDebounceTimer();
    }

    partial void OnLaunchOnStartupChanged(bool value) 
    {
        _userSettings.LaunchOnStartup = value;
        ResetSaveDebounceTimer();
    }

    partial void OnProgressBarColorChangeEnabledChanged(bool value)
    {
        _userSettings.IsProgressBarColorChangeEnabled = value;
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