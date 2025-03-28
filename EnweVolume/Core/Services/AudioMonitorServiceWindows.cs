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

    public event Action DeviceListChanged;
    public event Action<float> VolumeLevelChanged;
    public event Action DevicesChanged;

    public AudioMonitorServiceWindows()
    {
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
        var enumerator = new MMDeviceEnumerator();
        var deviceNameList = new List<string>();

        foreach (var device in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)) 
        {
            deviceNameList.Add(device.DeviceFriendlyName);
        }

        return deviceNameList;
    }

    public void SetDeviceByName(string deviceName)
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

    public void SetDeviceDefault()
    {
        var oldDevice = _audioDevice;
        _audioDevice = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        oldDevice?.Dispose();
        _isDefaultDevice = true;
    }

    public bool IsUsingDefaultDevice() => _isDefaultDevice;

    public void OnDeviceStateChanged(string deviceId, DeviceState newState)
    {
        DeviceListChanged?.Invoke();
    }

    public void OnDeviceAdded(string pwstrDeviceId)
    {
        DeviceListChanged?.Invoke();
    }

    public void OnDeviceRemoved(string deviceId)
    {
        DeviceListChanged?.Invoke();
    }

    public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
    {
        if (flow == DataFlow.Render && role == Role.Multimedia)
        {
            if (_isDefaultDevice)
            {
                SetDeviceDefault();
            }
            DeviceListChanged?.Invoke();
        }
    }

    public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key) { }

    private void CheckAudioLevels(object sender, EventArgs e)
    {
        if (_audioDevice == null)
        {
            return;
        }

        // 
        float currentPeak = _audioDevice.AudioMeterInformation.MasterPeakValue;
        float systemVolume = _audioDevice.AudioEndpointVolume.MasterVolumeLevelScalar;

        _latestAudioLevel = currentPeak * systemVolume;

        VolumeLevelChanged?.Invoke(_latestAudioLevel);
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
                _audioDevice?.Dispose();
            }

            disposed = true;
        }
    }
}
