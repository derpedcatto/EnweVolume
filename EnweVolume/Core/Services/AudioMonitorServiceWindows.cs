using EnweVolume.Core.Interfaces;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using System.Windows.Threading;

namespace EnweVolume.Core.Services;

public class AudioMonitorServiceWindows : IAudioMonitorService, IDisposable, IMMNotificationClient
{
    private readonly Lock _deviceLock = new();
    private readonly Dispatcher _dispatcher;
    private bool disposed = false;
    private bool _isDefaultDevice = false;
    private MMDeviceEnumerator? _deviceEnumerator;
    private MMDevice? _audioDevice;
    private DispatcherTimer? _volumeCheckTimer;
    private float _latestAudioLevel;

    public event Action DeviceListChanged;
    public event Action DefaultDeviceChanged;
    public event Action<float> VolumeLevelChanged;

    public AudioMonitorServiceWindows()
    {
        _dispatcher = Dispatcher.CurrentDispatcher;
        _deviceEnumerator = new MMDeviceEnumerator();
        _deviceEnumerator.RegisterEndpointNotificationCallback(this);
    }

    public void InitializeAudioMonitoring(int polling)
    {
        if (_audioDevice == null)
        {
            SetDeviceDefault();
        }

        StartTimer(polling);

        DeviceListChanged?.Invoke();
    }

    public bool IsUsingDefaultDevice()
    {
        lock(_deviceLock)
        { 
            return _isDefaultDevice; 
        }
    }

    public float GetLatestAudioLevel() => _latestAudioLevel;

    public string GetCurrentDeviceId()
    {
        lock (_deviceLock)
        {
            try
            {
                return _audioDevice?.ID ?? string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
    }

    public string GetCurrentDeviceName()
    {
        lock (_deviceLock)
        {
            try
            {
                return _audioDevice?.DeviceFriendlyName ?? string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
    }

    public string IdToName(string deviceId)
    {
        lock (_deviceLock)
        {
            try
            {
                var device = _deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
                    .SingleOrDefault(a => a.ID == deviceId);

                return device?.DeviceFriendlyName ?? string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
    }

    public string NameToId(string deviceFriendlyName)
    {
        lock (_deviceLock)
        {
            try
            {
                var device = _deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
                    .SingleOrDefault(a => a.DeviceFriendlyName == deviceFriendlyName);

                return device?.ID ?? string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
    }

    public List<string> GetAllDevicesId()
    {
        return _deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
                       .Select(d => d.ID)
                       .ToList() ?? [];
    }

    public List<string> GetAllDevicesName()
    {
        return _deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
                       .Select(d => d.DeviceFriendlyName)
                       .ToList() ?? [];
    }

    public void SetDeviceById(string deviceId)
    {
        MMDevice? newDevice;
        try
        {
            newDevice = _deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
                .FirstOrDefault(d => d.ID == deviceId);
        }
        catch (Exception)
        {
            return;
        }

        if (newDevice == null)
        {
            return;
        }

        _volumeCheckTimer?.Stop();
        lock (_deviceLock)
        {
            var oldDevice = _audioDevice;
            _audioDevice = newDevice;
            oldDevice?.Dispose();
            _isDefaultDevice = false;
        }

        if (_volumeCheckTimer != null)
        {
            StartTimer((int)_volumeCheckTimer.Interval.TotalMilliseconds);
        }
    }

    public void SetDeviceDefault()
    {
        MMDevice? newDevice;
        try
        {
            newDevice = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        }
        catch (Exception) 
        {
            // No devices
            return;
        }

        _volumeCheckTimer?.Stop();
        lock (_deviceLock)
        {
            var oldDevice = _audioDevice;

            // Check if new device is different from the current
            if (oldDevice == null || newDevice == null || oldDevice.ID != newDevice.ID)
            {
                _audioDevice = newDevice;
                oldDevice?.Dispose();
            }
            else
            {
                if (newDevice != null && newDevice != oldDevice)
                {
                    newDevice?.Dispose();
                }
            }
            _isDefaultDevice = (_audioDevice != null);
        }

        if (_volumeCheckTimer != null)
        {
            StartTimer((int)_volumeCheckTimer?.Interval.TotalMilliseconds);
        }
    }

    private void CheckAudioLevels(object sender, EventArgs e)
    {
        MMDevice? currentDevice;
        lock (_deviceLock)
        {
            currentDevice = _audioDevice;
        }

        if (currentDevice == null)
        {
            // Update only if audio level changed
            if (_latestAudioLevel != 0)
            {
                _latestAudioLevel = 0;
                VolumeLevelChanged?.Invoke(_latestAudioLevel);
            }
            return;
        }

        try
        {
            if (currentDevice.State != DeviceState.Active)
            {
                if (_latestAudioLevel != 0)
                {
                    VolumeLevelChanged?.Invoke(_latestAudioLevel);
                }
                return;
            }

            float currentPeak = currentDevice.AudioMeterInformation.MasterPeakValue;
            float systemVolume = currentDevice.AudioEndpointVolume.MasterVolumeLevelScalar;
            float newLevel = currentPeak * systemVolume;

            _latestAudioLevel = newLevel;

            VolumeLevelChanged?.Invoke(_latestAudioLevel);
        }
        catch (Exception)
        {
            if (_latestAudioLevel != 0)
            {
                _latestAudioLevel = 0;
                VolumeLevelChanged?.Invoke(_latestAudioLevel);
            }
        }
    }

    private void StartTimer(int polling)
    {
        _volumeCheckTimer?.Stop();

        _volumeCheckTimer = new DispatcherTimer(DispatcherPriority.Normal, _dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(polling)
        };
        _volumeCheckTimer.Tick += CheckAudioLevels;

        lock (_deviceLock)
        {
            if (_audioDevice != null && _audioDevice.State == DeviceState.Active) 
            {
                _volumeCheckTimer.Start();
            }
        }
    }

    #region IMMNotification Methods

    public void OnDeviceStateChanged(string deviceId, DeviceState newState)
    {
        _dispatcher.BeginInvoke(() => 
        {
            DeviceListChanged?.Invoke();

            lock (_deviceLock)
            {
                if (_audioDevice != null && _audioDevice.ID == deviceId && newState != DeviceState.Active)
                {
                    _volumeCheckTimer?.Stop();
                }
            }
        });
    }

    public void OnDeviceAdded(string pwstrDeviceId)
    {
        _dispatcher.BeginInvoke(() => DeviceListChanged?.Invoke());
    }

    public void OnDeviceRemoved(string deviceId)
    {
        _dispatcher.BeginInvoke(() =>
        {
            bool listChanged = false;
            lock (_deviceLock)
            {
                // If removed device was the one monitored
                if (_audioDevice != null && _audioDevice.ID == deviceId)
                {
                    _volumeCheckTimer?.Stop();
                    _audioDevice.Dispose();
                    _audioDevice = null;
                    _isDefaultDevice = false;
                    _latestAudioLevel = 0;
                    VolumeLevelChanged?.Invoke(_latestAudioLevel);
                    listChanged = true;
                    // Set default device?
                }
            }
            DeviceListChanged?.Invoke();
        });
    }

    public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
    {
        if (flow == DataFlow.Render && (role == Role.Multimedia || role == Role.Console))
        {
            _dispatcher.BeginInvoke(() =>
            {
                bool shouldUpdateDevice = false;

                lock (_deviceLock)
                {
                    shouldUpdateDevice = _isDefaultDevice;
                }

                if (shouldUpdateDevice)
                {
                    SetDeviceDefault();
                    DefaultDeviceChanged?.Invoke();
                }

                DeviceListChanged?.Invoke();
            });
        }
    }

    public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key) { }

    #endregion

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                _volumeCheckTimer?.Stop();
                _volumeCheckTimer = null;

                _deviceEnumerator?.UnregisterEndpointNotificationCallback(this);
                _deviceEnumerator?.Dispose();
                _deviceEnumerator = null;

                lock (_deviceLock)
                {
                    _audioDevice?.Dispose();
                    _audioDevice = null;
                }
            }
            disposed = true;
        }
    }

    ~AudioMonitorServiceWindows()
    {
        Dispose(false);
    }
}
