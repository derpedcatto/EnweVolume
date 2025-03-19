using EnweVolume.Core.Interfaces;
using EnweVolume.MVVM.ViewModels;
using System.Windows;

namespace EnweVolume.MVVM.Views;

/// <summary>
/// Interaction logic for SettingsWindow.xaml
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly IApplicationService _applicationService;
    private readonly SettingsViewModel _viewModel;

    public SettingsWindow(SettingsViewModel viewModel, IApplicationService applicationService)
    {
        InitializeComponent();
        _applicationService = applicationService;

        _viewModel = viewModel;
        DataContext = _viewModel;
        Loaded += async (s, e) => await _viewModel.Initialize();

        this.StateChanged += Window_StateChanged;
        this.Closing += Window_Closing;
    }

    private void Window_StateChanged(object sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            _applicationService.HideToTray();
        }
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = true;
        _applicationService.HideToTray();
    }

    private void VolumeBar_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_viewModel.VolumeBarSizeChangedCommand.CanExecute(e.NewSize.Width))
        {
            _viewModel.VolumeBarSizeChangedCommand.Execute(e.NewSize.Width);
        }
    }
}
