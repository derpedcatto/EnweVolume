using EnweVolume.Core.Interfaces;
using EnweVolume.Core.Models;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace EnweVolume.Core.Services;

public class UserSettingsService : IUserSettingsService
{
    public async Task<UserSettings> GetSettings()
    {
        if (!Directory.Exists(GetSettingsFolderPath()))
        {
            await GenerateSettings();
        }

        using FileStream openStream = File.OpenRead(GetSettingsFilePath());
        var json = await JsonSerializer.DeserializeAsync<UserSettings>(openStream);

        return json;
    }

    public async Task SaveSettings(UserSettings userSettings)
    {
        await using FileStream createStream = File.Create(GetSettingsFilePath());
        await JsonSerializer.SerializeAsync(createStream, userSettings);
    }

    private async Task GenerateSettings()
    {
        Directory.CreateDirectory(GetSettingsFolderPath());

        var systemCulture = CultureInfo.CurrentUICulture;
        var appCulture = App.SupportedCultures
                .Find(c => c.Name == systemCulture.Name)
                ?? App.SupportedCultures[0];

        var defaultSettings = new UserSettings()
        {
            AudioDeviceName = string.Empty,
            VolumeRedThresholdValue = 0.8f,
            VolumeYellowThresholdValue = 0.65f,
            NotificationRedPushEnabled = true,
            NotificationRedSoundEnabled = false,
            NotificationRedSoundVolume = 0.5f,
            NotificationYellowPushEnabled = false,
            NotificationYellowSoundEnabled = false,
            NotificationYellowSoundVolume = 0.5f,
            CurrentTheme = App.DefaultThemeName,
            ChangeProgressBarColorEnabled = true,
            StartWithSystemEnabled = true,
            Locale = appCulture.Name
        };

        await using FileStream createStream = File.Create(GetSettingsFilePath());
        await JsonSerializer.SerializeAsync(createStream, defaultSettings);
    }

    private static string GetSettingsFolderPath()
    {
        var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appDataFolder, App.AppName);
    }

    private static string GetSettingsFilePath()
    {
        var settingsFolder = GetSettingsFolderPath();
        return Path.Combine(settingsFolder, App.SettingsFileName);
    }
}
