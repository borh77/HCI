using System.Windows.Controls;
using StickItApp.Models;
using StickItApp.ViewModels;

namespace StickItApp.Views;

public partial class TagEditorPage : UserControl
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
}
