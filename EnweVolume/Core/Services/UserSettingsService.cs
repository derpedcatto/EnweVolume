using EnweVolume.Core.Enums;
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
                    return Result<UserSettings>.Failure(generateResult.Error!);
                }
            }

            return await DeserializeSettingsFile();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Result<UserSettings>.Failure(Error.From(
                ErrorCode.SettingsDirectoryAccessError,
                ex.Message
            ));
        }
        catch (Exception ex)
        {
            return Result<UserSettings>.Failure(Error.From(
                ErrorCode.Unknown,
                ex.Message
            ));
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
            return Result.Failure(
                Error.From(ErrorCode.InvalidUserSettings, "Settings object is null.")
            );
        }

        await _fileLock.WaitAsync();
        string tempFilePath = string.Empty;

        try
        {
            Directory.CreateDirectory(_settingsFolderPath);
            tempFilePath = Path.Combine(_settingsFolderPath, $"{Guid.NewGuid():N}.tmp");

            await using (var tmpStream = File.Create(tempFilePath))
            {
                await JsonSerializer.SerializeAsync(tmpStream, userSettings, _jsonSerializerOptions);
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
        catch (JsonException ex)
        {
            return Result.Failure(Error.From(
                ErrorCode.UserSettingsSaveError,
                ex.Message
            ));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Result<UserSettings>.Failure(Error.From(
                ErrorCode.SettingsDirectoryAccessError,
                ex.Message
            ));
        }
        catch (IOException ex)
        {
            return Result.Failure(Error.From(
                ErrorCode.UserSettingsSaveError,
                ex.Message
            ));
        }
        catch (Exception ex)
        {
            return Result.Failure(Error.From(
                ErrorCode.UserSettingsSaveError,
                ex.Message
            ));
        }   
        finally
        {
            if (!string.IsNullOrEmpty(tempFilePath) && File.Exists(tempFilePath))
            {
                try { File.Delete(tempFilePath); }
                catch { }
            }

            _fileLock.Release();
        }
    }

    public UserSettings GetDefaultUserSettings()
    {
        var systemCulture = CultureInfo.CurrentUICulture;
        var appCulture = App.SupportedCultures
                .Find(c => c.Name == systemCulture.Name)
                ?? App.SupportedCultures[0];

        return new UserSettings
        {
            SelectedLocale = appCulture.Name,
        };
    }

    private async Task<Result<UserSettings>> DeserializeSettingsFile()
    {
        try
        {
            using FileStream openStream = File.OpenRead(_settingsFilePath);
            var settings = await JsonSerializer.DeserializeAsync<UserSettings>(openStream)
                ?? throw new JsonException("Deserialized UserSettings to null");

            var defaultSettings = GetDefaultUserSettings();
            var validatedSettings = ValidateSettings(settings);

            if (!settings.Equals(validatedSettings))
            {
                await SaveSettings(validatedSettings);
            }

            return Result<UserSettings>.Success(settings);
        }
        catch (JsonException ex)
        {
            // Try to regenerate file
            File.Delete(_settingsFilePath); 
            var regenerateResult = await GenerateSettings();

            if (!regenerateResult.IsSuccess)
            {
                var error = Error.From(
                    regenerateResult.Error!.Code,
                    regenerateResult.Error.Message + (" | " + ex.Message));

                return Result<UserSettings>.Failure(error);
            }
            
            // Recurse once
            return await DeserializeSettingsFile();
        }
        catch (IOException ex)
        {
            return Result<UserSettings>.Failure(Error.From(
                    ErrorCode.UserSettingsLoadError,
                    ex.Message
                ));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Result<UserSettings>.Failure(Error.From(
                ErrorCode.SettingsDirectoryAccessError,
                ex.Message
            ));
        }
        catch (Exception ex)
        {
            return Result<UserSettings>.Failure(Error.From(
                ErrorCode.UserSettingsLoadError,
                ex.Message
            ));
        }
    }

    private async Task<Result> GenerateSettings()
    {
        try
        {
            Directory.CreateDirectory(GetSettingsFolderPath());
            var defaultSettings = GetDefaultUserSettings();

            await using (var stream = File.Create(_settingsFilePath))
            {
                await JsonSerializer.SerializeAsync(stream, defaultSettings, _jsonSerializerOptions);
            }

            return Result.Success();
        }
        catch (JsonException ex)
        {
            return Result<UserSettings>.Failure(Error.From(
                ErrorCode.UserSettingsSaveError,
                ex.Message
            ));
        }
        catch (IOException ex)
        {
            return Result<UserSettings>.Failure(Error.From(
                ErrorCode.UserSettingsSaveError,
                ex.Message
            ));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Result<UserSettings>.Failure(Error.From(
                ErrorCode.SettingsDirectoryAccessError,
                ex.Message
            ));
        }
        catch (Exception ex)
        {
            return Result<UserSettings>.Failure(Error.From(
                ErrorCode.UserSettingsSaveError,
                ex.Message
            ));
        }
    }

    private UserSettings ValidateSettings(UserSettings settings)
    {
        var defaultSettings = new DeviceSettings();

        foreach (var device in settings.DeviceProfiles.Values)
        {
            if (device.RedThresholdVolume < 1 ||
                device.RedThresholdVolume > 100 ||
                device.RedThresholdVolume <= device.YellowThresholdVolume)
            {
                device.RedThresholdVolume = defaultSettings.RedThresholdVolume;
            }

            if (device.YellowThresholdVolume < 0 ||
                device.YellowThresholdVolume > 99 ||
                device.YellowThresholdVolume >= device.RedThresholdVolume)
            {
                device.YellowThresholdVolume = defaultSettings.YellowThresholdVolume;
            }

            if (device.RedSoundNotificationVolume < 0 ||
                device.RedSoundNotificationVolume > 100)
            {
                device.RedSoundNotificationVolume = defaultSettings.RedSoundNotificationVolume;
            }

            if (device.YellowSoundNotificationVolume < 0 ||
                device.YellowSoundNotificationVolume > 100)
            {
                device.YellowSoundNotificationVolume = defaultSettings.YellowSoundNotificationVolume;
            }
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