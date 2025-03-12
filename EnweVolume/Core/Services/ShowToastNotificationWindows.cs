using EnweVolume.Core.Enums;
using EnweVolume.Core.Interfaces;
using Microsoft.Toolkit.Uwp.Notifications;

namespace EnweVolume.Core.Services;

class ShowToastNotificationWindows : IShowToastNotificationService
{
    public void Show(VolumeLevel volumeLevel)
    {
        switch (volumeLevel)
        {
            case VolumeLevel.Yellow:
                break;

            case VolumeLevel.Red:
                break;
        }
    }
}
