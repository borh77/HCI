using System.Windows.Controls;
using StickItApp.Models;
using StickItApp.ViewModels;

namespace StickItApp.Views;

public partial class TypeEditorPage : UserControl, IShortcutAwarePage
{
    public TypeEditorPage()
    {
        InitializeComponent();
    }

    public TypeEditorPage(MainWindowViewModel mainViewModel, EventType? type)
        : this()
    {
        DataContext = new TypeEditorViewModel(mainViewModel.NavigateToTypeList, type);
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
        if (DataContext is TypeEditorViewModel viewModel)
        {
            viewModel.CancelCommand.Execute(null);
            return true;
        }

        return false;
    }
}
