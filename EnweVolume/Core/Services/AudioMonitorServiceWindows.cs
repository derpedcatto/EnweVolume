using EnweVolume.Core.Interfaces;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using System.Windows.Threading;

namespace EnweVolume.Core.Services;

public class AudioMonitorServiceWindows : IAudioMonitorService, IDisposable, IMMNotificationClient
{
    private bool disposed = false;
    private bool _isDefaultDevice = false;
    private MMDeviceEnumerator _deviceEnumerator;
    private MMDevice? _audioDevice;
    private DispatcherTimer _volumeCheckTimer;
    private float _latestAudioLevel;
    private readonly object _deviceLock = new object();
    private readonly Dispatcher _dispatcher;

    public event Action DeviceListChanged;
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

        _volumeCheckTimer = new DispatcherTimer()
        {
            Interval = TimeSpan.FromMilliseconds(polling)
        };

        _volumeCheckTimer.Tick += CheckAudioLevels;
        _volumeCheckTimer.Start();
    }

    public float GetLatestAudioLevel() => _latestAudioLevel;

    public List<string> GetAllDeviceNames()
    {
        lock (_deviceLock)
        {
            var deviceNameList = new List<string>();
            foreach (var device in _deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
            {
                deviceNameList.Add(device.DeviceFriendlyName);
            }
            return deviceNameList;
        }
    }

    public void SetDeviceByName(string deviceName)
    {
        lock (_deviceLock)
        {
            foreach (var device in _deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
            {
                if (device.DeviceFriendlyName == deviceName)
                {
                    var oldDevice = _audioDevice;
                    _audioDevice = device;
                    oldDevice?.Dispose();
                    _isDefaultDevice = false;
                    break;
                }
            }
        }
    }

    public void SetDeviceDefault()
    {
        lock (_deviceLock)
        {
            var oldDevice = _audioDevice;
            _audioDevice = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            oldDevice?.Dispose();
            _isDefaultDevice = true;
        }
    }

    public bool IsUsingDefaultDevice() => _isDefaultDevice;

    public void OnDeviceStateChanged(string deviceId, DeviceState newState)
    {
        _dispatcher.BeginInvoke(() => DeviceListChanged?.Invoke());
    }

    public void OnDeviceAdded(string pwstrDeviceId)
    {
        _dispatcher.BeginInvoke(() => DeviceListChanged?.Invoke());
    }

    public void OnDeviceRemoved(string deviceId)
    {
        _dispatcher.BeginInvoke(() => DeviceListChanged?.Invoke());
    }

    public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
    {
        if (flow == DataFlow.Render && role == Role.Multimedia)
        {
            _dispatcher.BeginInvoke(() =>
            {
                lock (_deviceLock)
                {
                    if (_isDefaultDevice)
                    {
                        SetDeviceDefault();
                    }
                }
                DeviceListChanged?.Invoke();
            });
        }
    }

    public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key) { }

    private void CheckAudioLevels(object sender, EventArgs e)
    {
        MMDevice currentDevice;
        lock (_deviceLock)
        {
            currentDevice = _audioDevice;
        }

        if (currentDevice == null)
            return;
        try
        {
            float currentPeak = currentDevice.AudioMeterInformation.MasterPeakValue;
            float systemVolume = currentDevice.AudioEndpointVolume.MasterVolumeLevelScalar;

            _latestAudioLevel = currentPeak * systemVolume;

            VolumeLevelChanged?.Invoke(_latestAudioLevel);
        }
        catch (Exception ex)
        {
            lock (_deviceLock)
            {
                if (_audioDevice == currentDevice)
                {
                    _audioDevice?.Dispose();
                    _audioDevice = null;
                    _isDefaultDevice = false;
                }
            }
        }
    }

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
                // Dispose managed resources.
                _deviceEnumerator.UnregisterEndpointNotificationCallback(this);
                _deviceEnumerator.Dispose();
                _volumeCheckTimer?.Stop();
                lock (_deviceLock)
                {
                    _audioDevice?.Dispose();
                }
            }

            disposed = true;
        }
    }
}
