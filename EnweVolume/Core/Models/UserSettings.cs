namespace EnweVolume.Core.Models;

public class UserSettings
{
    public string AudioDeviceName { get; set; } = string.Empty;
    public int VolumeRedThresholdValue { get; set; }
    public int VolumeYellowThresholdValue { get; set; }
    public bool NotificationRedPushEnabled { get; set; }
    public bool NotificationRedSoundEnabled { get; set; }
    public int NotificationRedSoundVolume { get; set; }
    public bool NotificationYellowPushEnabled { get; set; }
    public bool NotificationYellowSoundEnabled { get; set; }
    public int NotificationYellowSoundVolume { get; set; }
    public bool ChangeProgressBarColorEnabled { get; set; }
    public bool StartWithSystemEnabled { get; set; }
    public string CurrentTheme { get; set; } = string.Empty;
    public string Locale { get; set; } = string.Empty;
}
