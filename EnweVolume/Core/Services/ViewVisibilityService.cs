using EnweVolume.Core.Interfaces;
using System.Windows;

namespace EnweVolume.Core.Services;

public class ViewVisibilityService : IViewVisibilityService
{
    public void HideMainWindow()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow == null) return;

            if (mainWindow.IsVisible)
            {
                HideMainWindow();
            }
            else
            {
                ShowMainWindow();
            }
        });
    }

    public void ShowMainWindow()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow != null)
            {
                mainWindow.Show();
                mainWindow.WindowState = WindowState.Normal;
                mainWindow.Activate();
            }
        });
    }

    public void ToggleMainWindowVisibility()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var mainWindow = Application.Current.MainWindow;
            mainWindow?.Hide();
        });
    }
}
