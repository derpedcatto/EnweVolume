using EnweVolume.Core.Enums;
using EnweVolume.Core.Interfaces;
using NAudio.CoreAudioApi;
using System.Windows.Threading;

namespace EnweVolume.Core.Services;

public class AudioMonitorService : IAudioMonitorService, IDisposable
{
    private bool disposed = false;
    private MMDevice _audioDevice;
    private DispatcherTimer _volumeCheckTimer;
    private VolumeLevel _volumeCurrentLevel;
    private float _volumeYellowThreshold;
    private float _volumeRedThreshold;

    public async Task InitializeAudioMonitoring(
        float volumeYellowThreshold,
        float volumeRedThreshold,
        string deviceName)
    {
        _volumeYellowThreshold = volumeYellowThreshold;
        _volumeRedThreshold = volumeRedThreshold;

        if (deviceName == string.Empty)
        {
            SetDeviceDefault();
        }
        else
        {
            SetDeviceByName(deviceName);
        }

        _volumeCheckTimer = new DispatcherTimer()
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        // _volumeCheckTimer.Tick += CheckAudioLevels;
        _volumeCheckTimer.Start();
    }

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
        var enumerator = new MMDeviceEnumerator();

        foreach (var device in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
        {
            if (device.DeviceFriendlyName == deviceName)
            {
                _audioDevice = device;
                break;
            }
        }
    }

    public void SetDeviceDefault()
    {
        var enumerator = new MMDeviceEnumerator();
        _audioDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
    }

    public void UpdateYellowThreshold(float newThreshold)
    {
        _volumeYellowThreshold = newThreshold;
    }

    public void UpdateRedThreshold(float newThreshold)
    {
        _volumeRedThreshold = newThreshold;
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
                _audioDevice?.Dispose();
            }

            disposed = true;
        }
    }
}
