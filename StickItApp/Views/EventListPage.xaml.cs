using System.Windows.Controls;
using StickItApp.ViewModels;

namespace StickItApp.Views;

public partial class EventListPage : UserControl, IShortcutAwarePage
{
    public EventListPage()
    {
        InitializeComponent();
    }

    public EventListPage(MainWindowViewModel mainViewModel)
        : this()
    {
        DataContext = new EventListViewModel(
            () => mainViewModel.NavigateToEventEditor(null),
            mainViewModel.NavigateToEventEditor,
            mainViewModel.NavigateToEventDetails,
            mainViewModel.SetStatus);
    }

    public bool FocusPrimarySearch()
    {
        FilterTextBox.Focus();
        FilterTextBox.SelectAll();
        return true;
    }

    public bool ResetFilters()
    {
        if (DataContext is EventListViewModel viewModel)
        {
            viewModel.ResetFilterCommand.Execute(null);
            return true;
        }

        return false;
    }

    public bool CancelOrBack()
    {
        return false;
    }
}
