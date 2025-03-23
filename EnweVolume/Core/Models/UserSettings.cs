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

    public override bool Equals(object? obj)
    {
        if (obj == null || GetType() != obj.GetType())
            return false;

        UserSettings other = (UserSettings)obj;
        return AudioDeviceName == other.AudioDeviceName &&
               VolumeRedThresholdValue == other.VolumeRedThresholdValue &&
               VolumeYellowThresholdValue == other.VolumeYellowThresholdValue &&
               NotificationRedPushEnabled == other.NotificationRedPushEnabled &&
               NotificationRedSoundEnabled == other.NotificationRedSoundEnabled &&
               NotificationRedSoundVolume == other.NotificationRedSoundVolume &&
               NotificationYellowPushEnabled == other.NotificationYellowPushEnabled &&
               NotificationYellowSoundEnabled == other.NotificationYellowSoundEnabled &&
               NotificationYellowSoundVolume == other.NotificationYellowSoundVolume &&
               ChangeProgressBarColorEnabled == other.ChangeProgressBarColorEnabled &&
               StartWithSystemEnabled == other.StartWithSystemEnabled &&
               CurrentTheme == other.CurrentTheme &&
               Locale == other.Locale;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            AudioDeviceName,
            VolumeRedThresholdValue,
            VolumeYellowThresholdValue,
            NotificationRedPushEnabled,
            NotificationRedSoundEnabled,
            NotificationRedSoundVolume,
            HashCode.Combine(
                NotificationYellowPushEnabled,
                NotificationYellowSoundEnabled,
                NotificationYellowSoundVolume,
                ChangeProgressBarColorEnabled,
                StartWithSystemEnabled,
                CurrentTheme,
                Locale
            )
        );
    }
}
