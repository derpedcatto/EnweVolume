using EnweVolume.Core.Enums;

namespace EnweVolume.Core.Models;

public class TrayIconTooltipData
{
    public VolumeLevel CurrentVolumeLevel { get; set; }
    public string DeviceName { get; set; } = string.Empty;
}
