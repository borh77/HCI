using System.Windows.Controls;
using StickItApp.ViewModels;

namespace StickItApp.Views;

public partial class TagListPage : UserControl
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
            mainViewModel.NavigateToTagEditor);
    }
}
