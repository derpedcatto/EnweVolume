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

public partial class SettingsViewModel : ObservableObject
{
    private readonly IMessenger _messenger;
    private readonly IShowToastNotificationService _showToastNotificationService;
    private readonly IAudioMonitorService _audioMonitorService;
    private readonly ITrayIconManager _trayIconManager;
    private readonly IUserSettingsService _userSettingsService;

    // private DispatcherTimer _volumeCheckUiTimer;
    private bool _notificationRedThresholdSent;
    private bool _notificationYellowThresholdSent;

    [ObservableProperty]
    private VolumeLevel _volumeCurrentLevel;

    [ObservableProperty]
    private Brush _volumeBarColor;

    [ObservableProperty]
    private float _volumeCurrentValue;

    [ObservableProperty]
    private float _volumeYellowThreshold;

    [ObservableProperty]
    private float _volumeRedThreshold;

    [ObservableProperty]
    private IEnumerable<string> _audioDeviceNamesList;

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
        VolumeYellowThreshold = 0.5f;
        VolumeRedThreshold = 0.75f;
    }

    partial void OnVolumeCurrentValueChanged(float oldValue, float newValue)
    {
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
}

/*
    private void InitializeAudioMonitoring()
    {
        try
        {
            var enumerator = new MMDeviceEnumerator();
            _audioDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

            // Start a timer to check audio levels periodically
            _volumeCheckTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(200)
            };
            _volumeCheckUiTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(75)
            };
            _volumeCheckTimer.Tick += CheckAudioLevels;
            _volumeCheckTimer.Start();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error initializing audio monitoring: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CheckAudioLevels(object sender, EventArgs e)
    {
        if (_audioDevice == null)
            return;

        float peakValue = _audioDevice.AudioMeterInformation.MasterPeakValue; // Get real-time peak level
        CurrentVolume = peakValue;
        IsVolumeTooHigh = peakValue > VolumeRedThreshold;

        if (IsVolumeTooHigh && !_notificationRedSent)
        {
            // ShowToastNotification("Warning", "Your audio volume is too high!");
            _notificationRedSent = true;
        }
        else if (!IsVolumeTooHigh)
        {
            _notificationRedSent = false; // Reset when volume goes back below threshold
        }
    }

    [RelayCommand]
    private void ExitApplication()
    {
        // Move to window logic (send message to window)
        _volumeCheckTimer?.Stop();
        _audioDeviceCurrent?.Dispose();
        Environment.Exit(0);
    }
 */