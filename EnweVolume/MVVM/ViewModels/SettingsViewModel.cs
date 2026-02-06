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
    private readonly string RESOURCE_KEY_DEFAULT_AUDIO_DEVICE = "String_DefaultAudioDevice";
    private readonly int SAVE_DEBOUNCE_TIMER_INTERVAL = 1000;
    private readonly int AUDIO_MONITORING_POLLING_RATE = 50;
    private readonly int UI_UPDATE_TIMER_INTERVAL = 50;

    private readonly IMessenger _messenger;
    private readonly IShowToastNotificationService _showToastNotificationService;
    private readonly IAudioMonitorService _audioMonitorService;
    private readonly ITrayIconManager _trayIconManager;
    private readonly IUserSettingsService _userSettingsService;

    private UserSettings _userSettings;
    private DeviceSettings _deviceSettings;
    private DispatcherTimer _uiUpdateTimer;
    private DispatcherTimer _saveDebounceTimer;
    private float _latestAudioLevel;
    private double _volumeBarWidth;

    public IRelayCommand<double> VolumeBarSizeChangedCommand { get; private set; }
    private Action DeviceListChangedAction { get; }

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
        DeviceListChangedAction = async () => await OnAudioDevicesChanged();
    }

    #region Initializers

    public async Task Initialize()
    {
        InitializeTimers();
        await InitializeUserSettings();
        await InitializeDeviceSettings();
        InitializeLocale();
        InitializeAudioMonitoring();
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
            await _userSettingsService.SaveSettings(_userSettings);
            // TODO: Notify that settings have not been loaded
        }

        ProgressBarColorChangeEnabled = _userSettings.IsProgressBarColorChangeEnabled;
        LaunchOnStartup = _userSettings.LaunchOnStartup;
    }

    // TODO: More complex checks
    private async Task InitializeDeviceSettings()
    {
        bool useDefaultAudioDevice = _userSettings.IsDefaultAudioDevice || string.IsNullOrEmpty(_userSettings.CurrentDeviceId);

        if (useDefaultAudioDevice)
        {
            var deviceDefaultResult = _audioMonitorService.SetDeviceDefault();
            if (!deviceDefaultResult.IsSuccess)
            {
                // ?
            }

            var currentDeviceIdResult = _audioMonitorService.GetCurrentDeviceId();
            if (!currentDeviceIdResult.IsSuccess || currentDeviceIdResult.Value == null)
            {
                return;
            }

            _userSettings.CurrentDeviceId = currentDeviceIdResult.Value;

            FetchCurrentAudioDeviceSettings();
        }
        else
        {
            _audioMonitorService.SetDeviceById(_userSettings.CurrentDeviceId);
            FetchCurrentAudioDeviceSettings();
        }

        await OnAudioDevicesChanged();
    }

    private void InitializeAudioMonitoring()
    {
        _audioMonitorService.InitializeAudioMonitoring(AUDIO_MONITORING_POLLING_RATE);
        _audioMonitorService.VolumeLevelChanged += OnAudioLevelChanged;
        _audioMonitorService.DeviceListChanged += DeviceListChangedAction;
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

    private void InitializeLocale()
    {
        LocaleList = App.SupportedCultures.Select(a => a.NativeName);

        var localeNameList = App.SupportedCultures.Select(a => a.Name);

        if (!localeNameList.Contains(_userSettings.SelectedLocale))
        {
            var defaultLocaleName = _userSettingsService.GetDefaultUserSettings().SelectedLocale;
        }
        var locale = App.SupportedCultures.FirstOrDefault(a => a.Name == _userSettings.SelectedLocale);
        SelectedLocale = locale!.NativeName;

        OnSelectedLocaleChanged(SelectedLocale);
    }

    #endregion

    // ? +
    private async Task SaveUserSettings()
    {
        try
        {
            _userSettings.DeviceProfiles[_userSettings.CurrentDeviceId] = _deviceSettings;
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

    private void ResetSaveDebounceTimer()
    {
        _saveDebounceTimer.Stop();
        _saveDebounceTimer.Start();
    }

    // ? +
    private void FetchCurrentAudioDeviceSettings()
    {
        var deviceId = _userSettings.CurrentDeviceId;
        _userSettings.DeviceProfiles.TryGetValue(deviceId, out _deviceSettings!);
        if (_deviceSettings == null)
        {
            _deviceSettings = new DeviceSettings();
            _userSettings.DeviceProfiles.Add(deviceId, _deviceSettings);
        }
    }

    private void OnAudioLevelChanged(float newLevel) => _latestAudioLevel = newLevel;
    
    // ? +
    private async Task OnAudioDevicesChanged()
    {
        var deviceListResult = _audioMonitorService.GetAllDevicesName();
        if (!deviceListResult.IsSuccess || deviceListResult.Value == null)
        {
            // !
        }

        var deviceList = new List<string>
        {
            App.GetString(RESOURCE_KEY_DEFAULT_AUDIO_DEVICE),
        };
        deviceList = [.. deviceList, .. deviceListResult.Value];

        AudioDeviceNames = deviceList;

        if (_userSettings.IsDefaultAudioDevice || !deviceList.Contains(SelectedAudioDevice))
        {
            await SaveUserSettings();
            SelectedAudioDevice = deviceList[0];
            _audioMonitorService.SetDeviceDefault();
            _userSettings.IsDefaultAudioDevice = true;

            var currentDeviceIdResult = _audioMonitorService.GetCurrentDeviceId();
            if (!currentDeviceIdResult.IsSuccess || currentDeviceIdResult.Value == null)
            {

            }
            else
            {
                _userSettings.CurrentDeviceId = currentDeviceIdResult.Value;
            }
        }
        else
        {
            _userSettings.IsDefaultAudioDevice = false;
        }

        FetchCurrentAudioDeviceSettings();
        UpdateBindedDeviceValues();
        ResetSaveDebounceTimer();
    }

    #region UI

    // ? +
    private void UpdateBindedDeviceValues()
    {
        RedThresholdVolume = _deviceSettings.RedThresholdVolume;
        YellowThresholdVolume = _deviceSettings.YellowThresholdVolume;
        YellowThresholdEnabled = _deviceSettings.IsYellowThresholdEnabled;
        RedPushNotificationEnabled = _deviceSettings.IsRedPushNotificationEnabled;
        RedSoundNotificationEnabled = _deviceSettings.IsRedSoundNotificationEnabled;
        RedSoundNotificationVolume = _deviceSettings.RedSoundNotificationVolume;
        YellowPushNotificationEnabled = _deviceSettings.IsYellowPushNotificationEnabled;
        YellowSoundNotificationEnabled = _deviceSettings.IsYellowSoundNotificationEnabled;
        YellowSoundNotificationVolume = _deviceSettings.YellowSoundNotificationVolume;
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

        _deviceSettings.RedThresholdVolume = value;
        ResetSaveDebounceTimer();
    }

    partial void OnYellowThresholdVolumeChanged(int value)
    {
        if (YellowThresholdEnabled && value >= RedThresholdVolume)
        {
            RedThresholdVolume = value + 1;
        }
        UpdateThresholdLinePositions();

        _deviceSettings.YellowThresholdVolume = value;
        ResetSaveDebounceTimer();
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

        _deviceSettings.IsYellowThresholdEnabled = value;
        ResetSaveDebounceTimer();
    }

    partial void OnRedPushNotificationEnabledChanged(bool value)
    {
        _deviceSettings.IsRedPushNotificationEnabled = value;
        ResetSaveDebounceTimer();
    }

    partial void OnRedSoundNotificationEnabledChanged(bool value)
    {
        _deviceSettings.IsRedSoundNotificationEnabled = value;
        ResetSaveDebounceTimer();
    }

    partial void OnRedSoundNotificationVolumeChanged(int value)
    {
        _deviceSettings.RedSoundNotificationVolume = value;
        ResetSaveDebounceTimer();
    }

    partial void OnYellowPushNotificationEnabledChanged(bool value) 
    {
        _deviceSettings.IsYellowPushNotificationEnabled = value;
        ResetSaveDebounceTimer();
    }

    partial void OnYellowSoundNotificationEnabledChanged(bool value)
    {
        _deviceSettings.IsYellowSoundNotificationEnabled = value;
        ResetSaveDebounceTimer();
    }

    partial void OnYellowSoundNotificationVolumeChanged(int value) 
    {
        _deviceSettings.YellowSoundNotificationVolume = value;
        ResetSaveDebounceTimer();
    }

    partial void OnSelectedAudioDeviceChanged(string value)
    {
        var defaultAudioDeviceName = App.GetString(RESOURCE_KEY_DEFAULT_AUDIO_DEVICE);

        if (value == defaultAudioDeviceName)
        {
            _audioMonitorService.SetDeviceDefault();
            _userSettings.IsDefaultAudioDevice = true;
        }
        else
        {
            var newDeviceIdResult = _audioMonitorService.NameToId(value);
            if (!newDeviceIdResult.IsSuccess || newDeviceIdResult.Value == null)
            {

            }
            else
            {
                _audioMonitorService.SetDeviceById(newDeviceIdResult.Value);
                _userSettings.IsDefaultAudioDevice = false;
            }
        }

        var currentDeviceIdResult = _audioMonitorService.GetCurrentDeviceId();
        if (!currentDeviceIdResult.IsSuccess || currentDeviceIdResult.Value == null)
        {
            //
        }
        else
        {
            _userSettings.CurrentDeviceId = currentDeviceIdResult.Value;
        }

        FetchCurrentAudioDeviceSettings();
        UpdateBindedDeviceValues();
        ResetSaveDebounceTimer();
    }

    // ? +
    partial void OnSelectedLocaleChanged(string value)
    {
        var selectedCulture = App.SupportedCultures.FirstOrDefault(c => c.NativeName == value);
        if (selectedCulture != null)
        {
            App.ApplyCulture(selectedCulture);
        }

        _userSettings.SelectedLocale = selectedCulture.Name;

        var newAudioDeviceList = AudioDeviceNames.ToList();
        newAudioDeviceList[0] = App.GetString(RESOURCE_KEY_DEFAULT_AUDIO_DEVICE);
        AudioDeviceNames = newAudioDeviceList;

        if (_audioMonitorService.IsUsingDefaultDevice())
        {
            SelectedAudioDevice = AudioDeviceNames.First();
        }

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
        _audioMonitorService.DeviceListChanged -= DeviceListChangedAction;

        _uiUpdateTimer?.Stop();

        _saveDebounceTimer.Stop();
        _ = SaveUserSettings().ConfigureAwait(false);
    }
}