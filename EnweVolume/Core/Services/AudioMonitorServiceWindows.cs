using EnweVolume.Core.Enums;
using EnweVolume.Core.Interfaces;
using EnweVolume.Core.Models;
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

    public Result InitializeAudioMonitoring(int polling)
    {
        if (_audioDevice == null)
        {
            var result = SetDeviceDefault();
            if (!result.IsSuccess)
            {
                return Result.Failure(result.Error);
            }
        }
        else // ?
        {
            var result = SetDeviceById(_audioDevice.ID);
            if (!result.IsSuccess)
            {
                return Result.Failure(result.Error);
            }
        }

        StartTimer(polling);
        DeviceListChanged?.Invoke();

        return Result.Success();
    }

    public bool IsUsingDefaultDevice()
    {
        lock(_deviceLock)
        { 
            return _isDefaultDevice; 
        }
    }

    public Result<float> GetLatestAudioLevel()
    {
        if (_audioDevice == null)
        {
            return Result<float>.Failure(
                new Error(ErrorType.Failure, ErrorCode.DeviceNotFound));
        }
        else
        {
            return Result<float>.Success(_latestAudioLevel);
        }
    }

    public Result<string> GetCurrentDeviceId()
    {
        lock (_deviceLock)
        {
            try
            {
                if (_audioDevice == null)
                {
                    return Result<string>.Failure(
                        new Error(ErrorType.Failure, ErrorCode.DeviceNotFound));
                }

                return Result<string>.Success(_audioDevice.ID);
            }
            catch (ObjectDisposedException ex)
            {
                return Result<string>.Failure(
                    new Error(ErrorType.Failure, ErrorCode.DeviceDisposed, ex.Message));
            }
            catch (Exception ex)
            {
                return Result<string>.Failure(
                    new Error(ErrorType.Failure, ErrorCode.Unknown, ex.Message));
            }
        }
    }

    public Result<string> GetCurrentDeviceName()
    {
        lock (_deviceLock)
        {
            try
            {
                if (_audioDevice == null)
                {
                    return Result<string>.Failure(
                        new Error(ErrorType.Failure, ErrorCode.DeviceNotFound));
                }

                return Result<string>.Success(_audioDevice.DeviceFriendlyName);
            }
            catch (ObjectDisposedException ex)
            {
                return Result<string>.Failure(
                    new Error(ErrorType.Failure, ErrorCode.DeviceDisposed, ex.Message));
            }
            catch (Exception ex)
            {
                return Result<string>.Failure(
                    new Error(ErrorType.Failure, ErrorCode.Unknown, ex.Message));
            }
        }
    }

    public Result<string> IdToName(string deviceId)
    {
        if (string.IsNullOrEmpty(deviceId))
        {
            return Result<string>.Failure(
                new Error(ErrorType.NotFound, ErrorCode.InvalidUserSettings));
        }

        lock (_deviceLock)
        {
            try
            {
                var device = _deviceEnumerator.GetDevice(deviceId);

                if (device == null)
                {
                    return Result<string>.Failure(
                        new Error(ErrorType.Failure, ErrorCode.DeviceNotFound));
                }

                return Result<string>.Success(device.DeviceFriendlyName);
            }
            catch (Exception ex)
            {
                return Result<string>.Failure(
                    new Error(ErrorType.Failure, ErrorCode.Unknown, ex.Message));
            }
        }
    }

    public Result<string> NameToId(string deviceFriendlyName)
    {
        if (string.IsNullOrEmpty(deviceFriendlyName))
        {
            return Result<string>.Failure(
                new Error(ErrorType.NotFound, ErrorCode.InvalidUserSettings));
        }

        lock (_deviceLock)
        {
            try
            {
                var device = _deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
                    .SingleOrDefault(a => a.FriendlyName == deviceFriendlyName);

                if (device == null)
                {
                    return Result<string>.Failure(
                        new Error(ErrorType.Failure, ErrorCode.DeviceNotFound));
                }

                return Result<string>.Success(device.DeviceFriendlyName);
            }
            catch (Exception ex)
            {
                return Result<string>.Failure(
                    new Error(ErrorType.Failure, ErrorCode.Unknown, ex.Message));
            }
        }
    }

    public Result<List<string>> GetAllDevicesId()
    {
        var list = _deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
                       .Select(d => d.ID)
                       .ToList() ?? [];

        return Result<List<string>>.Success(list);
    }

    public Result<List<string>> GetAllDevicesName()
    {
        var list = _deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
                       .Select(d => d.DeviceFriendlyName)
                       .ToList() ?? [];

        return Result<List<string>>.Success(list);
    }

    public Result SetDeviceById(string deviceId)
    {
        MMDevice? newDevice;
        try
        {
            var deviceListResult = GetAllDevicesId();
            if (!deviceListResult.IsSuccess)
            {
                return Result<string>.Failure(deviceListResult.Error);
            }

            var deviceList = deviceListResult.Value;

            newDevice = _deviceEnumerator.GetDevice(deviceId);
        }
        catch (ObjectDisposedException ex)
        {
            return Result<string>.Failure(
                new Error(ErrorType.Failure, ErrorCode.DeviceDisposed, ex.Message));
        }
        catch (Exception ex)
        {
            return Result<string>.Failure(
                new Error(ErrorType.Failure, ErrorCode.Unknown, ex.Message));
        }

        if (newDevice == null)
        {
            return Result<string>.Failure(
                new Error(ErrorType.Failure, ErrorCode.DeviceNotFound));
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

        return Result.Success();
    }

    public Result SetDeviceDefault()
    {
        MMDevice? newDevice;
        try
        {
            newDevice = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        }
        catch (Exception ex)
        {
            return Result<string>.Failure(
                new Error(ErrorType.Failure, ErrorCode.Unknown, ex.Message));
        }

        if (newDevice == null)
        {
            return Result<string>.Failure(
                new Error(ErrorType.Failure, ErrorCode.DeviceNotFound));
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

        return Result.Success();
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
