namespace EnweVolume.Core.Models;

public class UserSettings
{
    public Dictionary<string, DeviceSettings> DeviceProfiles { get; set; } = new();
    public string CurrentDeviceId { get; set; } = string.Empty;
    public bool IsProgressBarColorChangeEnabled { get; set; }
    public bool LaunchOnStartup { get; set; }
    public string Theme { get; set; } = string.Empty;
    public string SelectedLocale { get; set; } = string.Empty;

    public override bool Equals(object? obj)
    {
        if (obj == null || GetType() != obj.GetType())
            return false;

        UserSettings other = (UserSettings)obj;
        return IsProgressBarColorChangeEnabled == other.IsProgressBarColorChangeEnabled &&
               LaunchOnStartup == other.LaunchOnStartup &&
               Theme == other.Theme &&
               SelectedLocale == other.SelectedLocale &&
               DeviceProfiles.SequenceEqual(other.DeviceProfiles);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            IsProgressBarColorChangeEnabled,
            LaunchOnStartup,
            Theme,
            SelectedLocale,
            DeviceProfiles.Aggregate(0, (hash, kvp) => HashCode.Combine(hash, kvp.Key.GetHashCode(), kvp.Value.GetHashCode()))
        );
    }
}
