namespace EnweVolume.Core.Models;

public class UserSettings
{
    public Dictionary<string, DeviceSettings> DeviceProfiles { get; set; } = [];
    public string CurrentDeviceId { get; set; } = string.Empty;
    public bool IsDefaultAudioDevice { get; set; } = true;
    public bool IsProgressBarColorChangeEnabled { get; set; } = true;
    public bool LaunchOnStartup { get; set; } = true;
    public string Theme { get; set; } = App.DefaultThemeName;
    public string SelectedLocale { get; set; } = string.Empty;

    public override bool Equals(object? obj)
    {
        if (obj == null || GetType() != obj.GetType())
            return false;

        UserSettings other = (UserSettings)obj;
        return IsProgressBarColorChangeEnabled == other.IsProgressBarColorChangeEnabled &&
               CurrentDeviceId == other.CurrentDeviceId &&
               IsDefaultAudioDevice == other.IsDefaultAudioDevice &&
               LaunchOnStartup == other.LaunchOnStartup &&
               Theme == other.Theme &&
               SelectedLocale == other.SelectedLocale &&
               DeviceProfiles.SequenceEqual(other.DeviceProfiles);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            IsProgressBarColorChangeEnabled,
            CurrentDeviceId,
            IsDefaultAudioDevice,
            LaunchOnStartup,
            Theme,
            SelectedLocale,
            DeviceProfiles.Aggregate(0, (hash, kvp) => HashCode.Combine(hash, kvp.Key.GetHashCode(), kvp.Value.GetHashCode()))
        );
    }
}
