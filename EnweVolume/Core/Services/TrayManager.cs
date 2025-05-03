using EnweVolume.Core.Enums;
using EnweVolume.Core.Interfaces;
using EnweVolume.Core.Models;
using Hardcodet.Wpf.TaskbarNotification;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace EnweVolume.Core.Services;

public class TrayManager : ITrayManager
{
    private bool disposedValue;
    private TaskbarIcon _taskbarIcon;
    private IReadOnlyDictionary<VolumeLevel, Uri> _iconSet;
    private MenuItem _startWithSystemMenuItem;
    private VolumeLevel _currentVolumeLevel = VolumeLevel.Green;

    public event EventHandler TrayIconLeftClicked;
    public event EventHandler ExitRequested;
    public event EventHandler<bool> StartWithSystemToggled;

    public Result Initialize(IReadOnlyDictionary<VolumeLevel, Uri> iconSet, bool isLaunchOnStartupEnabled)
    {
        _iconSet = iconSet;

        Application.Current.Dispatcher.Invoke(() =>
        {
            _taskbarIcon = new TaskbarIcon
            {
                ToolTipText = "...",
            };

            var contextMenu = new ContextMenu();

            _startWithSystemMenuItem = new MenuItem
            {
                Header = "Start with System",
                IsCheckable = true,
                IsChecked = isLaunchOnStartupEnabled,
            };
            _startWithSystemMenuItem.Click += StartWithSystemMenuItem_Click;

            var exitMenuItem = new MenuItem
            {
                Header = "Exit",
            };
            exitMenuItem.Click += ExitMenuItem_Click;

            contextMenu.Items.Add(_startWithSystemMenuItem);
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(exitMenuItem);

            _taskbarIcon.ContextMenu = contextMenu;

            _taskbarIcon.TrayLeftMouseDown += TaskbarIcon_TrayLeftMouseDown;
            SetIcon(VolumeLevel.Green);
        });

        return Result.Success();
    }

    public Result SetIcon(VolumeLevel volumeLevel)
    {
        if (_iconSet.TryGetValue(volumeLevel, out var iconUri))
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _taskbarIcon.IconSource = new BitmapImage(iconUri);
                });
                _currentVolumeLevel = volumeLevel;
                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure(
                    new Error(ErrorType.Failure, ErrorCode.Unknown, ex.Message));
            }
        }
        else
        {
            return Result.Failure(new Error(ErrorType.Failure, ErrorCode.Unknown));
        }
    }

    public Result ChangeIconSet(IReadOnlyDictionary<VolumeLevel, Uri> newIconSet)
    {
        _iconSet = newIconSet;
        return SetIcon(_currentVolumeLevel);
    }

    public void SetIconTooltip(TrayIconTooltipData data)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            _taskbarIcon.ToolTipText = data?.ToString() ?? "...";
        });
    }

    public void SetStartWithSystemChecked(bool isChecked)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            _startWithSystemMenuItem.IsChecked = isChecked;
        });
    }

    private void TaskbarIcon_TrayLeftMouseDown(object sender, RoutedEventArgs e)
    {
        TrayIconLeftClicked?.Invoke(this, EventArgs.Empty);
    }

    private void StartWithSystemMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem)
        {
            StartWithSystemToggled?.Invoke(this, menuItem.IsChecked);
        }
    }

    private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
    {
        ExitRequested?.Invoke(this, EventArgs.Empty);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                if (_taskbarIcon != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _taskbarIcon.TrayLeftMouseDown -= TaskbarIcon_TrayLeftMouseDown;
                        if (_startWithSystemMenuItem != null)
                            _startWithSystemMenuItem.Click -= StartWithSystemMenuItem_Click;

                        _taskbarIcon.Dispose();
                        _taskbarIcon = null;
                    });
                }
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    ~TrayManager()
    {
        Dispose(false);
    }
}
