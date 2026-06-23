using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using StickItApp.Commands;
using StickItApp.Models;

namespace StickItApp.ViewModels;

public sealed class TagListViewModel : ObservableObject
{
    private readonly Action _addTag;
    private readonly Action<Tag> _editTag;
    private string _filterText = string.Empty;
    private string _selectedSortMode = "Code";

    public TagListViewModel(Action addTag, Action<Tag> editTag)
    {
        _addTag = addTag;
        _editTag = editTag;
        TagsView = CollectionViewSource.GetDefaultView(App.DataStore.Tags);
        TagsView.Filter = FilterTag;
        ApplySort();

        AddCommand = new RelayCommand(_addTag);
        EditCommand = new RelayCommand(parameter =>
        {
            if (parameter is Tag tag)
            {
                _editTag(tag);
            }
        });
        DeleteCommand = new RelayCommand(DeleteTag);
        ResetFilterCommand = new RelayCommand(() =>
        {
            FilterText = string.Empty;
            SelectedSortMode = "Code";
        });
    }

    public ICollectionView TagsView { get; }

    public IReadOnlyList<string> SortModes { get; } = ["Code", "Name"];

    public string FilterText
    {
        get => _filterText;
        set
        {
            if (SetProperty(ref _filterText, value))
            {
                TagsView.Refresh();
            }
        }
    }

    public string SelectedSortMode
    {
        get => _selectedSortMode;
        set
        {
            if (SetProperty(ref _selectedSortMode, value))
            {
                ApplySort();
            }
        }
    }

    public ICommand AddCommand { get; }

    public ICommand EditCommand { get; }

    public ICommand DeleteCommand { get; }

    public ICommand ResetFilterCommand { get; }

    private bool FilterTag(object item)
    {
        if (item is not Tag tag || string.IsNullOrWhiteSpace(FilterText))
        {
            return true;
        }

        string query = FilterText.Trim();
        return Contains(tag.Id, query) ||
               Contains(tag.Name, query);
    }

    private static bool Contains(string value, string query)
    {
        return value.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private void ApplySort()
    {
        TagsView.SortDescriptions.Clear();
        TagsView.SortDescriptions.Add(new SortDescription(
            SelectedSortMode == "Name" ? nameof(Tag.Name) : nameof(Tag.Id),
            ListSortDirection.Ascending));
        TagsView.Refresh();
    }

    private void DeleteTag(object? parameter)
    {
        if (parameter is not Tag tag)
        {
            return;
        }

        App.DataStore.Tags.Remove(tag);
        List<EventTag> relations = App.DataStore.EventTags.Where(item => item.TagId == tag.Id).ToList();
        foreach (EventTag relation in relations)
        {
            App.DataStore.EventTags.Remove(relation);
        }

        App.DataService.SaveAll(App.DataStore);
        TagsView.Refresh();
        MessageBox.Show("Tag deleted. Related event-tag links were removed.", "Delete tag", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
