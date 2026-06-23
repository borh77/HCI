using System.Windows.Controls;
using StickItApp.ViewModels;

namespace StickItApp.Views;

public partial class TypeListPage : UserControl, IShortcutAwarePage
{
    public TypeListPage()
    {
        InitializeComponent();
    }

    public TypeListPage(MainWindowViewModel mainViewModel)
        : this()
    {
        DataContext = new TypeListViewModel(
            () => mainViewModel.NavigateToTypeEditor(null),
            mainViewModel.NavigateToTypeEditor);
    }

    public bool FocusPrimarySearch()
    {
        FilterTextBox.Focus();
        FilterTextBox.SelectAll();
        return true;
    }

    public bool ResetFilters()
    {
        if (DataContext is TypeListViewModel viewModel)
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
