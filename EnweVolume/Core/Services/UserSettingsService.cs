using EnweVolume.Core.Interfaces;
using EnweVolume.Core.Models;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace EnweVolume.Core.Services;

public class UserSettingsService : IUserSettingsService
{
    private static readonly SemaphoreSlim _fileLock = new(1, 1);
    private readonly string _settingsFolderPath;
    private readonly string _settingsFilePath;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private bool _disposed;

    public UserSettingsService()
    {
        _settingsFolderPath = GetSettingsFolderPath();
        _settingsFilePath = GetSettingsFilePath();

        _jsonSerializerOptions = new()
        {
            WriteIndented = true,
        };
    }

    public async Task<Result<UserSettings>> GetSettings()
    {
        await _fileLock.WaitAsync();
        try
        {
            if (!File.Exists(_settingsFilePath))
            {
                var generateResult = await GenerateSettings();
                if (!generateResult.IsSuccess)
                {
                    return Result<UserSettings>.Failure(
                        generateResult.Caption,
                        generateResult.Message);
                }
            }

            return await DeserializeSettingsFile();
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<Result> SaveSettings(UserSettings userSettings)
    {
        if (userSettings == null)
        {
            return Result.Failure("Invalid Settings", "User settings cannot be null.");
        }

        await _fileLock.WaitAsync();
        try
        {
            var tempFileName = Guid.NewGuid().ToString() + ".tmp";
            var tempFilePath = Path.Combine(_settingsFolderPath, tempFileName);

            try
            {
                Directory.CreateDirectory(_settingsFolderPath);

                // Creating temp file
                await using (var tempStream = File.Create(tempFilePath))
                {
                    await JsonSerializer.SerializeAsync(tempStream, userSettings, _jsonSerializerOptions);
                }

                if (File.Exists(_settingsFilePath))
                {
                    File.Replace(tempFilePath, _settingsFilePath, null);
                }
                else
                {
                    File.Move(tempFilePath, _settingsFilePath);
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure("Save Failed", $"Failed to save settings: {ex.Message}");
            }
            finally
            {
                if (File.Exists(tempFilePath))
                {
                    try
                    {
                        File.Delete(tempFilePath);
                    }
                    catch (Exception)
                    {
                        // Not doing anything if deleting temp file is not possible
                        // Logger would go here if necessary but it's an overkill for this app lol
                    }
                }
            }
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public UserSettings GetDefaultSettings()
    {
        var systemCulture = CultureInfo.CurrentUICulture;
        var appCulture = App.SupportedCultures
                .Find(c => c.Name == systemCulture.Name)
                ?? App.SupportedCultures[0];

        return new UserSettings()
        {
            AudioDeviceName = string.Empty,
            VolumeRedThresholdValue = 80,
            VolumeYellowThresholdValue = 65,
            NotificationRedPushEnabled = true,
            NotificationRedSoundEnabled = false,
            NotificationRedSoundVolume = 50,
            NotificationYellowPushEnabled = false,
            NotificationYellowSoundEnabled = false,
            NotificationYellowSoundVolume = 50,
            CurrentTheme = App.DefaultThemeName,
            ChangeProgressBarColorEnabled = true,
            StartWithSystemEnabled = true,
            Locale = appCulture.Name
        };
    }

    private async Task<Result<UserSettings>> DeserializeSettingsFile()
    {
        try
        {
            using FileStream openStream = File.OpenRead(_settingsFilePath);
            var settings = await JsonSerializer.DeserializeAsync<UserSettings>(openStream);

            if (settings == null)
            {
                return Result<UserSettings>.Failure(
                    "Deserialization Failed",
                    "Settings file could not be deserialized properly.");
            }

            var defaultSettings = GetDefaultSettings();
            var validatedSettings = ValidateSettings(settings);

            if (!settings.Equals(validatedSettings))
            {
                await SaveSettings(validatedSettings);
            }

            return Result<UserSettings>.Success(settings);
        }
        catch (JsonException)
        {
            // Handling corrupt settings by regenerating file
            try
            {
                File.Delete(_settingsFilePath); 
                var regenerateResult = await GenerateSettings();
                if (!regenerateResult.IsSuccess)
                {
                    return Result<UserSettings>.Failure(regenerateResult.Caption, regenerateResult.Message);
                }
                // return await DeserializeSettingsFile();
            }
            catch (Exception ex)
            {
                return Result<UserSettings>.Failure(
                    "Settings Recovery Failed",
                    $"Failed to recover from corrupt settings file: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            return Result<UserSettings>.Failure(
                "Settings Read Failed",
                $"Failed to read settings file: {ex.Message}");
        }

        return Result<UserSettings>.Failure(
            "Unexpected Error",
            "Unexpected error during deserialization");
    }

    private async Task<Result> GenerateSettings()
    {
        try
        {
            Directory.CreateDirectory(GetSettingsFolderPath());

            var defaultSettings = GetDefaultSettings();

            await using FileStream createStream = File.Create(_settingsFilePath);
            await JsonSerializer.SerializeAsync(createStream, defaultSettings, _jsonSerializerOptions);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(
                "Settings Generation Failed",
                $"Failed to generate default settings: {ex.Message}");
        }
    }

    private UserSettings ValidateSettings(UserSettings settings)
    {
        // TODO: Validation logic

        var defaultSettings = GetDefaultSettings();

        if (settings.VolumeRedThresholdValue < 0 || settings.VolumeRedThresholdValue > 100)
        {
            settings.VolumeRedThresholdValue = defaultSettings.VolumeRedThresholdValue;
        }

        if (settings.VolumeYellowThresholdValue < 0 || settings.VolumeYellowThresholdValue > 100)
        {
            settings.VolumeYellowThresholdValue = defaultSettings.VolumeYellowThresholdValue;
        }

        if (settings.NotificationRedSoundVolume < 0 || settings.NotificationRedSoundVolume > 100)
        {
            settings.NotificationRedSoundVolume = defaultSettings.NotificationRedSoundVolume;
        }

        if (settings.NotificationYellowSoundVolume < 0 || settings.NotificationYellowSoundVolume > 100)
        {
            settings.NotificationYellowSoundVolume = defaultSettings.NotificationYellowSoundVolume;
        }

        return settings;
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

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _fileLock.Dispose();
        }

        _disposed = true;
    }
}