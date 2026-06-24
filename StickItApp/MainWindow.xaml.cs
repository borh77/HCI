using System.Windows;
using System.Windows.Input;
using StickItApp.ViewModels;
using StickItApp.Views;

namespace StickItApp;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainWindowViewModel();
        DataContext = _viewModel;
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);

        bool isCtrlDown = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
        if (isCtrlDown && e.Key == Key.N)
        {
            _viewModel.NavigateToEventEditor(null);
            e.Handled = true;
            return;
        }

        if (isCtrlDown && e.Key == Key.F)
        {
            if (_viewModel.CurrentPage is IShortcutAwarePage page)
            {
                page.FocusPrimarySearch();
            }

            e.Handled = true;
            return;
        }

        if (isCtrlDown && e.Key == Key.R)
        {
            if (_viewModel.CurrentPage is IShortcutAwarePage page)
            {
                page.ResetFilters();
            }

            e.Handled = true;
            return;
        }

        if (isCtrlDown && e.Key == Key.M)
        {
            _viewModel.ShowMapCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape)
        {
            if (_viewModel.IsMenuOpen)
            {
                _viewModel.IsMenuOpen = false;
                e.Handled = true;
                return;
            }

            if (_viewModel.CurrentPage is IShortcutAwarePage page)
            {
                e.Handled = page.CancelOrBack();
            }
        }
    }

    private void DrawerBackdrop_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _viewModel.IsMenuOpen = false;
        e.Handled = true;
    }
}
