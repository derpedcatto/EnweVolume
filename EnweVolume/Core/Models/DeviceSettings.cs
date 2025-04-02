namespace EnweVolume.Core.Models;

public class DeviceSettings
{
    public string AudioDeviceId { get; set; } = string.Empty;
    public int RedThresholdVolume { get; set; }
    public int YellowThresholdVolume { get; set; }
    public bool IsYellowThresholdEnabled { get; set; }
    public bool IsRedPushNotificationEnabled { get; set; }
    public bool IsRedSoundNotificationEnabled { get; set; }
    public int RedSoundNotificationVolume { get; set; }
    public bool IsYellowPushNotificationEnabled { get; set; }
    public bool IsYellowSoundNotificationEnabled { get; set; }
    public int YellowSoundNotificationVolume { get; set; }

    public override bool Equals(object? obj)
    {
        if (obj == null || GetType() != obj.GetType())
            return false;

        DeviceSettings other = (DeviceSettings)obj;
        return AudioDeviceId == other.AudioDeviceId &&
               RedThresholdVolume == other.RedThresholdVolume &&
               YellowThresholdVolume == other.YellowThresholdVolume &&
               IsYellowThresholdEnabled == other.IsYellowThresholdEnabled &&
               IsRedPushNotificationEnabled == other.IsRedPushNotificationEnabled &&
               IsRedSoundNotificationEnabled == other.IsRedSoundNotificationEnabled &&
               RedSoundNotificationVolume == other.RedSoundNotificationVolume &&
               IsYellowPushNotificationEnabled == other.IsYellowPushNotificationEnabled &&
               IsYellowSoundNotificationEnabled == other.IsYellowSoundNotificationEnabled &&
               YellowSoundNotificationVolume == other.YellowSoundNotificationVolume;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            AudioDeviceId,
            RedThresholdVolume,
            YellowThresholdVolume,
            IsYellowThresholdEnabled,
            IsRedPushNotificationEnabled,
            IsRedSoundNotificationEnabled,
            RedSoundNotificationVolume,
            HashCode.Combine(
                IsYellowPushNotificationEnabled,
                IsYellowSoundNotificationEnabled,
                YellowSoundNotificationVolume
            )
        );
    }
}
