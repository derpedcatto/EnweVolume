namespace EnweVolume.Core.Interfaces;

public interface IAudioMonitorService
{
    public void InitializeAudioMonitoring(int polling);
    public List<string> GetAllDeviceNames();
    public void SetDeviceByName(string deviceName);
    public void SetDeviceDefault();

    event Action<float> VolumeLevelChanged;
}
