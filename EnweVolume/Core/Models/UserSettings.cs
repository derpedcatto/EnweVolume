namespace EnweVolume.Core.Models;

public class UserSettings
{
    public Dictionary<string, DeviceSettings> DeviceProfiles { get; set; } = new();
    public string CurrentDeviceName { get; set; } = string.Empty;
    public bool ChangeProgressBarColorEnabled { get; set; }
    public bool StartWithSystemEnabled { get; set; }
    public string CurrentTheme { get; set; } = string.Empty;
    public string Locale { get; set; } = string.Empty;

    public override bool Equals(object? obj)
    {
        if (obj == null || GetType() != obj.GetType())
            return false;

        UserSettings other = (UserSettings)obj;
        return ChangeProgressBarColorEnabled == other.ChangeProgressBarColorEnabled &&
               StartWithSystemEnabled == other.StartWithSystemEnabled &&
               CurrentTheme == other.CurrentTheme &&
               Locale == other.Locale &&
               DeviceProfiles.SequenceEqual(other.DeviceProfiles);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            ChangeProgressBarColorEnabled,
            StartWithSystemEnabled,
            CurrentTheme,
            Locale,
            DeviceProfiles.Aggregate(0, (hash, kvp) => HashCode.Combine(hash, kvp.Key.GetHashCode(), kvp.Value.GetHashCode()))
        );
    }
}
