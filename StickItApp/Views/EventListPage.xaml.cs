using System.Windows.Controls;
using StickItApp.ViewModels;

namespace StickItApp.Views;

public partial class EventListPage : UserControl
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
            mainViewModel.NavigateToEventDetails);
    }
}
