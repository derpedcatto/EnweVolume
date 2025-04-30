namespace EnweVolume.Core.Enums;

public enum ErrorCode
{
    Unknown = 0,

    // Validation
    InvalidUserSettings = 100,

    // IO
    UserSettingsSaveError = 200,
    UserSettingsLoadError = 201,
    SettingsFileCorrupted = 202,
    SettingsFileLockError = 203,

    // Device
    DeviceNotFound = 300,
    DeviceAccessDenied = 301,
    DefaultDeviceUnavailable = 302,

    // Access
    AccessDenied = 400,
    SettingsDirectoryAccessError = 401,
}
