using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
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

    private void DatePicker_CalendarOpened(object sender, RoutedEventArgs e)
    {
        if (sender is not DatePicker datePicker)
        {
            return;
        }

        datePicker.Dispatcher.BeginInvoke(() =>
        {
            datePicker.ApplyTemplate();
            if (datePicker.Template.FindName("PART_Popup", datePicker) is not Popup popup)
            {
                return;
            }

            popup.PlacementTarget = datePicker;
            popup.Placement = PlacementMode.Top;
            popup.VerticalOffset = -4;
            popup.HorizontalOffset = 0;

            if (popup.Child is FrameworkElement child)
            {
                child.MaxWidth = 222;
                child.MaxHeight = 245;
            }
        }, DispatcherPriority.Loaded);
    }
}
