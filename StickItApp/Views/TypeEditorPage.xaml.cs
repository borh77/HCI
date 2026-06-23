using System.Windows.Controls;
using StickItApp.Models;
using StickItApp.ViewModels;

namespace StickItApp.Views;

public partial class TypeEditorPage : UserControl
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
}
