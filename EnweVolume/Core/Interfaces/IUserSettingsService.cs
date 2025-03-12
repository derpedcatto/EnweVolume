using EnweVolume.Core.Models;

namespace EnweVolume.Core.Interfaces;

public interface IUserSettingsService
{
    Task<UserSettings> GetSettings();
    Task SaveSettings(UserSettings userSettings);
}
