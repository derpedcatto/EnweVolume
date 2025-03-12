namespace EnweVolume.Core.Models;

public struct UserSettings
{
    public string AudioDeviceName { get; set; }
    public float VolumeRedThresholdValue { get; set; }
    public float VolumeYellowThresholdValue { get; set; }
    public bool NotificationRedPushEnabled { get; set; }
    public bool NotificationRedSoundEnabled { get; set; }
    public float NotificationRedSoundVolume { get; set; }
    public bool NotificationYellowPushEnabled { get; set; }
    public bool NotificationYellowSoundEnabled { get; set; }
    public float NotificationYellowSoundVolume { get; set; }
    public string CurrentTheme { get; set; }
    public string Locale { get; set; }
}
