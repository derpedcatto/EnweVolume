using NAudio.CoreAudioApi;

namespace EnweVolume.Core.Interfaces;

public interface IAudioMonitorService
{
    public Task InitializeAudioMonitoring(float volumeYellowThreshold, float volumeRedThreshold, string deviceName);
    public List<string> GetAllDeviceNames();
    public void SetDeviceByName(string deviceName);
    public void SetDeviceDefault();
    public void UpdateYellowThreshold(float newThreshold);
    public void UpdateRedThreshold(float newThreshold);
}
