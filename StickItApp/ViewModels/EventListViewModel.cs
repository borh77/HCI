using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using StickItApp.Commands;
using StickItApp.Models;

namespace StickItApp.ViewModels;

public sealed class EventListViewModel : ObservableObject
{
    private readonly Action _addEvent;
    private readonly Action<Event> _editEvent;
    private readonly Action<Event> _showDetails;
    private readonly Action<string, bool>? _showStatus;
    private string _filterText = string.Empty;
    private string _selectedSortMode = "Name";

    public EventListViewModel(Action addEvent, Action<Event> editEvent, Action<Event> showDetails, Action<string, bool>? showStatus = null)
    {
        _addEvent = addEvent;
        _editEvent = editEvent;
        _showDetails = showDetails;
        _showStatus = showStatus;

        RebuildItems();
        EventsView = CollectionViewSource.GetDefaultView(Items);
        EventsView.Filter = FilterEvent;
        ApplySort();

        AddCommand = new RelayCommand(_addEvent);
        DetailsCommand = new RelayCommand(parameter => RunForEvent(parameter, _showDetails));
        EditCommand = new RelayCommand(parameter => RunForEvent(parameter, _editEvent));
        DeleteCommand = new RelayCommand(DeleteEvent);
        ToggleMapCommand = new RelayCommand(ToggleMap);
        ResetFilterCommand = new RelayCommand(() =>
        {
            FilterText = string.Empty;
            SelectedSortMode = "Name";
        });
    }

    public ObservableCollection<EventListItemViewModel> Items { get; } = [];

    public ICollectionView EventsView { get; }

    public IReadOnlyList<string> SortModes { get; } = ["Name", "Code", "City", "Attendance"];

    public string FilterText
    {
        get => _filterText;
        set
        {
            if (SetProperty(ref _filterText, value))
            {
                EventsView.Refresh();
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

    public ICommand DetailsCommand { get; }

    public ICommand EditCommand { get; }

    public ICommand DeleteCommand { get; }

    public ICommand ToggleMapCommand { get; }

    public ICommand ResetFilterCommand { get; }

    private void RebuildItems()
    {
        Items.Clear();
        foreach (Event eventItem in App.DataStore.Events)
        {
            Items.Add(new EventListItemViewModel(eventItem));
        }
    }

    private bool FilterEvent(object item)
    {
        if (item is not EventListItemViewModel eventItem || string.IsNullOrWhiteSpace(FilterText))
        {
            return true;
        }

        string query = FilterText.Trim();
        return Contains(eventItem.Id, query) ||
               Contains(eventItem.Name, query) ||
               Contains(eventItem.Description, query) ||
               Contains(eventItem.City, query) ||
               Contains(eventItem.Country, query);
    }

    private static bool Contains(string value, string query)
    {
        return value.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private void ApplySort()
    {
        EventsView.SortDescriptions.Clear();
        string propertyName = SelectedSortMode switch
        {
            "Code" => nameof(EventListItemViewModel.Id),
            "City" => nameof(EventListItemViewModel.City),
            "Attendance" => nameof(EventListItemViewModel.Attendance),
            _ => nameof(EventListItemViewModel.Name)
        };
        EventsView.SortDescriptions.Add(new SortDescription(propertyName, ListSortDirection.Ascending));
        EventsView.Refresh();
    }

    private static void RunForEvent(object? parameter, Action<Event> action)
    {
        if (parameter is EventListItemViewModel item)
        {
            action(item.Event);
        }
    }

    private void DeleteEvent(object? parameter)
    {
        if (parameter is not EventListItemViewModel item)
        {
            return;
        }

        MessageBoxResult result = MessageBox.Show(
            $"Delete event '{item.Name}'?",
            "Delete event",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        App.DataStore.Events.Remove(item.Event);
        RemoveRelations(item.Event.Id);
        App.DataService.SaveAll(App.DataStore);
        Items.Remove(item);
        EventsView.Refresh();
        _showStatus?.Invoke($"Event '{item.Name}' deleted.", false);
    }

    private void ToggleMap(object? parameter)
    {
        if (parameter is not EventListItemViewModel item)
        {
            return;
        }

        item.Event.IsPlacedOnMap = !item.Event.IsPlacedOnMap;
        App.DataService.SaveAll(App.DataStore);
        RebuildItems();
        EventsView.Refresh();
    }

    private static void RemoveRelations(string eventId)
    {
        foreach (EventTag relation in App.DataStore.EventTags.Where(item => item.EventId == eventId).ToList())
        {
            App.DataStore.EventTags.Remove(relation);
        }

        foreach (PreviousDate previousDate in App.DataStore.PreviousDates.Where(item => item.EventId == eventId).ToList())
        {
            App.DataStore.PreviousDates.Remove(previousDate);
        }
    }
}
