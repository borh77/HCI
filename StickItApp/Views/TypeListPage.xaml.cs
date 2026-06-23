using System.Windows.Controls;
using StickItApp.ViewModels;

namespace StickItApp.Views;

public partial class TypeListPage : UserControl
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
}
