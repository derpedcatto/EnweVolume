using EnweVolume.Core.Enums;
using EnweVolume.Core.Models;

namespace EnweVolume.Core.Interfaces;

public interface ITrayManager : IDisposable
{
    event EventHandler TrayIconLeftClicked;
    event EventHandler ExitRequested;
    event EventHandler<bool> StartWithSystemToggled;

    Result Initialize(IReadOnlyDictionary<VolumeLevel, Uri> iconSet, bool isLaunchOnStartupEnabled);
    Result SetIcon(VolumeLevel volumeLevel);
    Result ChangeIconSet(IReadOnlyDictionary<VolumeLevel, Uri> newIconSet);
    void SetIconTooltip(TrayIconTooltipData data);
    void SetStartWithSystemChecked(bool isChecked);
}
