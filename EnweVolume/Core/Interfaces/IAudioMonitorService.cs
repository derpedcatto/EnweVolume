namespace EnweVolume.Core.Interfaces;

public interface IAudioMonitorService
{
    public void InitializeAudioMonitoring(int polling);
    public float GetLatestAudioLevel();
    public List<string> GetAllDeviceNames();
    public void SetDeviceByName(string deviceName);
    public void SetDeviceDefault();
    public bool IsUsingDefaultDevice();

    event Action DevicesChanged;
    event Action<float> VolumeLevelChanged;
}
