using System.Windows.Controls;
using StickItApp.Models;
using StickItApp.ViewModels;

namespace StickItApp.Views;

public partial class TagEditorPage : UserControl, IShortcutAwarePage
{
    public TagEditorPage()
    {
        InitializeComponent();
    }

    public TagEditorPage(MainWindowViewModel mainViewModel, Tag? tag)
        : this()
    {
        DataContext = new TagEditorViewModel(mainViewModel.NavigateToTagList, tag);
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
        if (DataContext is TagEditorViewModel viewModel)
        {
            viewModel.CancelCommand.Execute(null);
            return true;
        }

        return false;
    }
}
