namespace EnweVolume.Core.Models;

public class DeviceSettings
{
    public int RedThresholdVolume { get; set; } = 80;
    public int YellowThresholdVolume { get; set; } = 65;
    public bool IsYellowThresholdEnabled { get; set; } = false;
    public bool IsRedPushNotificationEnabled { get; set; } = true;
    public bool IsRedSoundNotificationEnabled { get; set; } = false;
    public int RedSoundNotificationVolume { get; set; } = 50;
    public bool IsYellowPushNotificationEnabled { get; set; } = false;
    public bool IsYellowSoundNotificationEnabled { get; set; } = false;
    public int YellowSoundNotificationVolume { get; set; } = 50;

    public override bool Equals(object? obj)
    {
        if (obj == null || GetType() != obj.GetType())
            return false;

        DeviceSettings other = (DeviceSettings)obj;
        return RedThresholdVolume == other.RedThresholdVolume &&
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
