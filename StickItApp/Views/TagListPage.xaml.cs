using System.Windows.Controls;
using StickItApp.ViewModels;

namespace StickItApp.Views;

public partial class TagListPage : UserControl, IShortcutAwarePage
{
    public TagListPage()
    {
        InitializeComponent();
    }

    public TagListPage(MainWindowViewModel mainViewModel)
        : this()
    {
        DataContext = new TagListViewModel(
            () => mainViewModel.NavigateToTagEditor(null),
            mainViewModel.NavigateToTagEditor,
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
        if (DataContext is TagListViewModel viewModel)
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
