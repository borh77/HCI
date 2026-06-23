using System.Windows.Controls;
using StickItApp.Models;
using StickItApp.ViewModels;

namespace StickItApp.Views;

public partial class EventDetailsPage : UserControl
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
}
