using System.Windows.Controls;
using StickItApp.Models;
using StickItApp.ViewModels;

namespace StickItApp.Views;

public partial class EventDetailsPage : UserControl, IShortcutAwarePage
{
    public EventDetailsPage()
    {
        InitializeComponent();
    }

    public EventDetailsPage(MainWindowViewModel mainViewModel, Event eventItem)
        : this()
    {
        DataContext = new EventDetailsViewModel(
            eventItem,
            mainViewModel.NavigateToEventList,
            mainViewModel.NavigateToEventEditor);
    }

    public bool FocusPrimarySearch()
    {
        return false;
    }

    public bool ResetFilters()
    {
        return false;
    }

    public bool CancelOrBack()
    {
        if (DataContext is EventDetailsViewModel viewModel)
        {
            viewModel.BackCommand.Execute(null);
            return true;
        }

        return false;
    }
}
