namespace EnweVolume.Core.Interfaces;

public interface IAudioMonitorService
{
    public void InitializeAudioMonitoring(int polling);
    public bool IsUsingDefaultDevice();
    public float GetLatestAudioLevel();
    public List<string> GetAllDevicesId();
    public List<string> GetAllDevicesName();
    public string GetCurrentDeviceId();
    public string GetCurrentDeviceName();
    public void SetDeviceById(string deviceId);
    public void SetDeviceDefault();
    public string IdToName(string deviceId);
    public string NameToId(string deviceFriendlyName);

    event Action DeviceListChanged;
    event Action DefaultDeviceChanged;
    event Action<float> VolumeLevelChanged;
}
