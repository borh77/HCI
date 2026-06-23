using System.Windows.Controls;
using StickItApp.Models;
using StickItApp.ViewModels;

namespace StickItApp.Views;

public partial class EventEditorPage : UserControl, IShortcutAwarePage
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

    public bool FocusPrimarySearch()
    {
        TagSearchTextBox.Focus();
        TagSearchTextBox.SelectAll();
        return true;
    }

    public bool ResetFilters()
    {
        if (DataContext is EventEditorViewModel viewModel)
        {
            viewModel.TagSearchText = string.Empty;
            return true;
        }

        return false;
    }

    public bool CancelOrBack()
    {
        if (DataContext is EventEditorViewModel viewModel)
        {
            viewModel.CancelCommand.Execute(null);
            return true;
        }

        return false;
    }
}
