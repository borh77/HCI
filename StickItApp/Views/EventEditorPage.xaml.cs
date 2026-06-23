using System.Windows.Controls;
using StickItApp.Models;
using StickItApp.ViewModels;

namespace StickItApp.Views;

public partial class EventEditorPage : UserControl
{
    public EventEditorPage()
    {
        InitializeComponent();
    }

    public EventEditorPage(MainWindowViewModel mainViewModel, Event? eventItem)
        : this()
    {
        DataContext = new EventEditorViewModel(
            mainViewModel.NavigateToEventList,
            () => mainViewModel.NavigateToTypeEditor(null),
            () => mainViewModel.NavigateToTagEditor(null),
            eventItem);
    }
}
