using EnweVolume.MVVM.ViewModels;
using System.Windows;

namespace EnweVolume.MVVM.Views;

/// <summary>
/// Interaction logic for SettingsWindow.xaml
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;

    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        Loaded += async (s, e) => await _viewModel.Initialize();
    }

    private void VolumeBar_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_viewModel.VolumeBarSizeChangedCommand.CanExecute(e.NewSize.Width))
        {
            _viewModel.VolumeBarSizeChangedCommand.Execute(e.NewSize.Width);
        }
    }
}
