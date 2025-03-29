using EnweVolume.Core.Models;

namespace EnweVolume.Core.Interfaces;

public interface IUserSettingsService
{
    Task<Result<UserSettings>> GetSettings();
    Task<Result> SaveSettings(UserSettings userSettings);
    UserSettings GetDefaultUserSettings();
    DeviceSettings GetDefaultDeviceSettings(string deviceName);
}
