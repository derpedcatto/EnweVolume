namespace EnweVolume.Core.Enums;

public enum ErrorCode
{
    Unknown = 0,

    // Validation
    InvalidUserSettings,

    // IO
    UserSettingsSaveError,
    UserSettingsLoadError,
    SettingsFileCorrupted,
    SettingsFileLockError,
    SettingsDirectoryAccessError,

    // Device
    DeviceNotFound,
    DeviceAccessDenied,
    DeviceDisposed,

    // Runtime
    OperationCanceled,
    PermissionDenied,
}
