using EnweVolume.Core.Models;

namespace EnweVolume.Core.Interfaces;

public interface IAudioMonitorService : IDisposable
{
    public Result InitializeAudioMonitoring(int polling);
    public bool IsUsingDefaultDevice();
    public Result<float> GetLatestAudioLevel();
    public Result<List<string>> GetAllDevicesId();
    public Result<List<string>> GetAllDevicesName();
    public Result<string> GetCurrentDeviceId();
    public Result<string> GetCurrentDeviceName();
    public Result SetDeviceById(string deviceId);
    public Result SetDeviceDefault();
    public Result<string> IdToName(string deviceId);
    public Result<string> NameToId(string deviceFriendlyName);

    event Action DeviceListChanged;
    event Action DefaultDeviceChanged;
    event Action<float> VolumeLevelChanged;
}
