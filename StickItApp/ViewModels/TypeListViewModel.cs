using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using StickItApp.Commands;
using StickItApp.Models;

namespace StickItApp.ViewModels;

public sealed class TypeListViewModel : ObservableObject
{
    private readonly Action _addType;
    private readonly Action<EventType> _editType;
    private string _filterText = string.Empty;
    private string _selectedSortMode = "Code";

    public TypeListViewModel(Action addType, Action<EventType> editType)
    {
        _addType = addType;
        _editType = editType;
        TypesView = CollectionViewSource.GetDefaultView(App.DataStore.EventTypes);
        TypesView.Filter = FilterType;
        ApplySort();

        AddCommand = new RelayCommand(_addType);
        EditCommand = new RelayCommand(parameter =>
        {
            if (parameter is EventType type)
            {
                _editType(type);
            }
        });
        DeleteCommand = new RelayCommand(DeleteType);
        ResetFilterCommand = new RelayCommand(() =>
        {
            FilterText = string.Empty;
            SelectedSortMode = "Code";
        });
    }

    public ICollectionView TypesView { get; }

    public IReadOnlyList<string> SortModes { get; } = ["Code", "Name"];

    public string FilterText
    {
        get => _filterText;
        set
        {
            if (SetProperty(ref _filterText, value))
            {
                TypesView.Refresh();
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

    private bool FilterType(object item)
    {
        if (item is not EventType type || string.IsNullOrWhiteSpace(FilterText))
        {
            return true;
        }

        string query = FilterText.Trim();
        return Contains(type.Id, query) ||
               Contains(type.Name, query) ||
               Contains(type.Description, query);
    }

    private static bool Contains(string value, string query)
    {
        return value.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private void ApplySort()
    {
        TypesView.SortDescriptions.Clear();
        TypesView.SortDescriptions.Add(new SortDescription(
            SelectedSortMode == "Name" ? nameof(EventType.Name) : nameof(EventType.Id),
            ListSortDirection.Ascending));
        TypesView.Refresh();
    }

    private void DeleteType(object? parameter)
    {
        if (parameter is not EventType type)
        {
            return;
        }

        if (App.DataStore.Events.Any(item => item.TypeId == type.Id))
        {
            MessageBox.Show(
                "This type is used by one or more events and cannot be deleted.",
                "Delete type",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        App.DataStore.EventTypes.Remove(type);
        App.DataService.SaveAll(App.DataStore);
        TypesView.Refresh();
        MessageBox.Show("Type deleted.", "Delete type", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
