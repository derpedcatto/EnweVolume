namespace EnweVolume.Core.Interfaces;

/// <summary>
/// Manages application lifecycle and handles background operations.
/// </summary>
public interface IApplicationService
{
    void Initialize();
    void ShowSettingsWindow();
    void HideToTray();
    void Exit();
}
