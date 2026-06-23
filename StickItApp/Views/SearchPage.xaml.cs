using System.Windows.Controls;
using StickItApp.ViewModels;

namespace StickItApp.Views;

public partial class SearchPage : UserControl, IShortcutAwarePage
{
    private MainWindowViewModel? _mainViewModel;

    public SearchPage()
    {
        InitializeComponent();
    }

    public SearchPage(MainWindowViewModel mainViewModel)
        : this()
    {
        _mainViewModel = mainViewModel;
        DataContext = new SearchViewModel(
            mainViewModel.NavigateToEventDetails,
            mainViewModel.NavigateToEventEditor);
    }

    public bool FocusPrimarySearch()
    {
        PrimarySearchBox.Focus();
        PrimarySearchBox.SelectAll();
        return true;
    }

    public bool ResetFilters()
    {
        if (DataContext is SearchViewModel viewModel)
        {
            viewModel.Reset();
            return true;
        }

        return false;
    }

    public bool CancelOrBack()
    {
        _mainViewModel?.NavigateToEventList();
        return _mainViewModel is not null;
    }
}
