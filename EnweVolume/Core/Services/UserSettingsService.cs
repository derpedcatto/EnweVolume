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
                    return Result<UserSettings>.Failure(generateResult.Error);
                }
            }

            return await DeserializeSettingsFile();
        }
        catch (UnauthorizedAccessException uaEx)
        {
            return Result<UserSettings>.Failure(new Error(
                ErrorType.AccessForbidden,
                ErrorCode.AccessDenied,
                $"Unauthorized permission error: {uaEx.Message}"
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
                new Error(ErrorType.Validation, ErrorCode.InvalidUserSettings, "Settings object is null."));
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
                File.Replace(tempFilePath, _settingsFilePath, null);
            else
                File.Move(tempFilePath, _settingsFilePath);

            return Result.Success();
        }
        catch (JsonException jsonEx)
        {
            return Result.Failure(new Error(
                ErrorType.Failure,
                ErrorCode.UserSettingsSaveError,
                $"Json error: {jsonEx.Message}"
            ));
        }
        catch (UnauthorizedAccessException uaEx)
        {
            return Result<UserSettings>.Failure(new Error(
                ErrorType.AccessForbidden,
                ErrorCode.SettingsDirectoryAccessError,
                $"Unauthorized permission error: {uaEx.Message}"
            ));
        }
        catch (IOException ioEx)
        {
            return Result.Failure(new Error(
                ErrorType.Failure,
                ErrorCode.UserSettingsSaveError,
                $"I/O error: {ioEx.Message}"
            ));
        }
        catch (Exception ex)
        {
            return Result.Failure(new Error(
                ErrorType.Failure,
                ErrorCode.UserSettingsSaveError,
                ex.Message
            ));
        }
        finally
        {
            if (tempFilePath != null && File.Exists(tempFilePath))
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

        return new UserSettings()
        {
            DeviceProfiles = [],
            CurrentDeviceId = string.Empty,
            IsDefaultAudioDevice = true,
            Theme = App.DefaultThemeName,
            IsProgressBarColorChangeEnabled = true,
            LaunchOnStartup = true,
            SelectedLocale = appCulture.Name
        };
    }

    public DeviceSettings GetDefaultDeviceSettings(string deviceId)
    {
        return new DeviceSettings()
        {
            RedThresholdVolume = 80,
            YellowThresholdVolume = 65,
            IsYellowThresholdEnabled = false,
            IsRedPushNotificationEnabled = true,
            IsRedSoundNotificationEnabled = false,
            RedSoundNotificationVolume = 50,
            IsYellowPushNotificationEnabled = false,
            IsYellowSoundNotificationEnabled = false,
            YellowSoundNotificationVolume = 50,
        };
    }

    private async Task<Result<UserSettings>> DeserializeSettingsFile()
    {
        try
        {
            using FileStream openStream = File.OpenRead(_settingsFilePath);
            var settings = await JsonSerializer.DeserializeAsync<UserSettings>(openStream)
                ?? throw new JsonException("Deserialized to null");

            var defaultSettings = GetDefaultUserSettings();
            var validatedSettings = ValidateSettings(settings);

            if (!settings.Equals(validatedSettings))
            {
                await SaveSettings(validatedSettings);
            }

            return Result<UserSettings>.Success(settings);
        }
        catch (JsonException jsonEx)
        {
            // Try to regenerate file
            File.Delete(_settingsFilePath); 
            var regenerateResult = await GenerateSettings();
            if (!regenerateResult.IsSuccess)
            {
                var error = new Error(
                    regenerateResult.Error.ErrorType,
                    regenerateResult.Error.Code,
                    regenerateResult.Error.DebugDescription + (" | " + jsonEx.Message));

                return Result<UserSettings>.Failure(error);
            }
            
            // Recurse once
            return await DeserializeSettingsFile();
        }
        catch (IOException ioEx)
        {
            return Result<UserSettings>.Failure(new Error(
                    ErrorType.Failure,
                    ErrorCode.UserSettingsLoadError,
                    ioEx.Message
                ));
        }
        catch (UnauthorizedAccessException uaEx)
        {
            return Result<UserSettings>.Failure(new Error(
                ErrorType.AccessForbidden,
                ErrorCode.AccessDenied,
                $"Unauthorized permission error: {uaEx.Message}"
            ));
        }
        catch (Exception ex)
        {
            return Result<UserSettings>.Failure(new Error(
                ErrorType.Failure, 
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
        catch (JsonException jsonEx)
        {
            return Result<UserSettings>.Failure(new Error(
                ErrorType.Failure,
                ErrorCode.UserSettingsSaveError,
                $"Json error: {jsonEx.Message}"
            ));
        }
        catch (IOException ioEx)
        {
            return Result<UserSettings>.Failure(new Error(
                ErrorType.Failure,
                ErrorCode.UserSettingsSaveError,
                $"IO error: {ioEx.Message}"
            ));
        }
        catch (UnauthorizedAccessException uaEx)
        {
            return Result<UserSettings>.Failure(new Error(
                ErrorType.AccessForbidden,
                ErrorCode.AccessDenied,
                $"Unauthorized permission error: {uaEx.Message}"
            ));
        }
        catch (Exception ex)
        {
            return Result<UserSettings>.Failure(new Error(
                ErrorType.Failure,
                ErrorCode.UserSettingsSaveError,
                $"{ex.Message}"
            ));
        }
    }

    private UserSettings ValidateSettings(UserSettings settings)
    {
        var defaultSettings = GetDefaultDeviceSettings(string.Empty);

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