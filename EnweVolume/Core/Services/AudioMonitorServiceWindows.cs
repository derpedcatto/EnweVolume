using EnweVolume.Core.Converters;
using EnweVolume.Core.Interfaces;
using NAudio.CoreAudioApi;
using System.Windows.Threading;

namespace EnweVolume.Core.Services;

public class AudioMonitorServiceWindows : IAudioMonitorService, IDisposable
{
    private bool disposed = false;
    private MMDevice? _audioDevice;
    private DispatcherTimer _volumeCheckTimer;
    private float _latestAudioLevel;

    public event Action<float> VolumeLevelChanged;

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

    private void CheckAudioLevels(object sender, EventArgs e)
    {
        if (_audioDevice == null)
        {
            return;
        }

        float currentPeak = _audioDevice.AudioMeterInformation.MasterPeakValue;
        float systemVolume = _audioDevice.AudioEndpointVolume.MasterVolumeLevelScalar;

        _latestAudioLevel = AudioLevelConverter.PeakValueToDbSPL(currentPeak, systemVolume);

        VolumeLevelChanged?.Invoke(_latestAudioLevel);
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
                _volumeCheckTimer?.Stop();
                _audioDevice?.Dispose();
            }

            disposed = true;
        }
    }
}
