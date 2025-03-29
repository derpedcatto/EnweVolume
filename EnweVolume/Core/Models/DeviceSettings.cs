namespace EnweVolume.Core.Models;

public class DeviceSettings
{
    public string AudioDeviceName { get; set; } = string.Empty;
    public int VolumeRedThresholdValue { get; set; }
    public int VolumeYellowThresholdValue { get; set; }
    public bool VolumeYellowThresholdEnabled { get; set; }
    public bool NotificationRedPushEnabled { get; set; }
    public bool NotificationRedSoundEnabled { get; set; }
    public int NotificationRedSoundVolume { get; set; }
    public bool NotificationYellowPushEnabled { get; set; }
    public bool NotificationYellowSoundEnabled { get; set; }
    public int NotificationYellowSoundVolume { get; set; }

    public override bool Equals(object? obj)
    {
        if (obj == null || GetType() != obj.GetType())
            return false;

        DeviceSettings other = (DeviceSettings)obj;
        return AudioDeviceName == other.AudioDeviceName &&
               VolumeRedThresholdValue == other.VolumeRedThresholdValue &&
               VolumeYellowThresholdValue == other.VolumeYellowThresholdValue &&
               VolumeYellowThresholdEnabled == other.VolumeYellowThresholdEnabled &&
               NotificationRedPushEnabled == other.NotificationRedPushEnabled &&
               NotificationRedSoundEnabled == other.NotificationRedSoundEnabled &&
               NotificationRedSoundVolume == other.NotificationRedSoundVolume &&
               NotificationYellowPushEnabled == other.NotificationYellowPushEnabled &&
               NotificationYellowSoundEnabled == other.NotificationYellowSoundEnabled &&
               NotificationYellowSoundVolume == other.NotificationYellowSoundVolume;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            AudioDeviceName,
            VolumeRedThresholdValue,
            VolumeYellowThresholdValue,
            VolumeYellowThresholdEnabled,
            NotificationRedPushEnabled,
            NotificationRedSoundEnabled,
            NotificationRedSoundVolume,
            HashCode.Combine(
                NotificationYellowPushEnabled,
                NotificationYellowSoundEnabled,
                NotificationYellowSoundVolume
            )
        );
    }
}
