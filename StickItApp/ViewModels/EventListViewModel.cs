using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using StickItApp.Commands;
using StickItApp.Models;
using StickItApp.Services;

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
        Event eventModel = eventItem.Event;
        EventType? type = App.DataStore.EventTypes.FirstOrDefault(typeItem => typeItem.Id == eventModel.TypeId);

        return MatchesAny(
                   query,
                   eventModel.Id,
                   $"#{eventModel.Id}",
                   eventModel.Name,
                   eventModel.City,
                   eventModel.Country,
                   GetLocationSearchValue(eventModel),
                   eventModel.Description,
                   eventModel.TypeId,
                   type?.Id,
                   type?.Name,
                   type?.Description,
                   eventModel.AverageCost.ToString(CultureInfo.CurrentCulture),
                   eventModel.AverageCost.ToString(CultureInfo.InvariantCulture)) ||
               MatchesAny(query, GetAttendanceSearchValues(eventModel)) ||
               MatchesCharitable(query, eventModel) ||
               MatchesAny(query, GetDateSearchValues(eventModel)) ||
               MatchesAny(query, GetTagSearchValues(eventModel)) ||
               MatchesMapPlacement(query, eventModel);
    }

    private static bool Matches(string? value, string query)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               value.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesAny(string query, params string?[] values)
    {
        return values.Any(value => Matches(value, query));
    }

    private static string? GetLocationSearchValue(Event eventItem)
    {
        if (string.IsNullOrWhiteSpace(eventItem.City) && string.IsNullOrWhiteSpace(eventItem.Country))
        {
            return null;
        }

        return $"{eventItem.City}, {eventItem.Country}".Trim(' ', ',');
    }

    private static string?[] GetAttendanceSearchValues(Event eventItem)
    {
        return eventItem.Attendance switch
        {
            AttendanceCategory.UpTo1000 => [eventItem.Attendance.ToString(), "1000", "<= 1000", "Up to 1000"],
            AttendanceCategory.From1000To5000 => [eventItem.Attendance.ToString(), "1000-5000", "1000 to 5000"],
            AttendanceCategory.From5000To10000 => [eventItem.Attendance.ToString(), "5000-10000", "5000 to 10000"],
            AttendanceCategory.Over10000 => [eventItem.Attendance.ToString(), "10000+", "> 10000", "Over 10000"],
            _ => [eventItem.Attendance.ToString()]
        };
    }

    private static string?[] GetCharitableSearchValues(Event eventItem)
    {
        return eventItem.IsCharitable
            ? ["true", "yes", "charitable", "charity", "humanitarno", "dobrotvorno", "da"]
            : ["false", "no", "not charitable", "not charity", "nije humanitarno", "nije dobrotvorno", "ne"];
    }

    private static bool MatchesCharitable(string query, Event eventItem)
    {
        string normalizedQuery = query.Trim().ToLowerInvariant();
        string[] charitableValues = ["true", "yes", "charitable", "charity", "humanitarno", "dobrotvorno", "da"];
        string[] nonCharitableValues = ["false", "no", "not charitable", "not charity", "nije humanitarno", "nije dobrotvorno", "ne"];

        if (charitableValues.Contains(normalizedQuery))
        {
            return eventItem.IsCharitable;
        }

        if (nonCharitableValues.Contains(normalizedQuery))
        {
            return !eventItem.IsCharitable;
        }

        return MatchesAny(query, GetCharitableSearchValues(eventItem));
    }

    private static string?[] GetDateSearchValues(Event eventItem)
    {
        List<string?> values = [];
        AddDateSearchValues(values, eventItem.Date);
        AddDateSearchValues(values, eventItem.CurrentStart);
        AddDateSearchValues(values, eventItem.CurrentEnd);

        foreach (PreviousDate previousDate in App.DataStore.PreviousDates.Where(item => item.EventId == eventItem.Id))
        {
            AddDateSearchValues(values, previousDate.Date);
            AddDateSearchValues(values, previousDate.Start);
            AddDateSearchValues(values, previousDate.End);
        }

        return [.. values];
    }

    private static void AddDateSearchValues(List<string?> values, DateTime date)
    {
        if (date == default)
        {
            return;
        }

        values.AddRange(FormatDateForSearch(date));
    }

    private static string?[] FormatDateForSearch(DateTime date)
    {
        return
        [
            date.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
            date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
            date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            date.ToString("yyyy", CultureInfo.InvariantCulture)
        ];
    }

    private static string?[] GetTagSearchValues(Event eventItem)
    {
        return
        [
            .. App.DataStore.EventTags
                .Where(relation => relation.EventId == eventItem.Id)
                .SelectMany(relation =>
                {
                    Tag? tag = App.DataStore.Tags.FirstOrDefault(item => item.Id == relation.TagId);
                    return new[]
                    {
                        relation.TagId,
                        tag?.Id,
                        tag?.Name,
                        tag?.Description,
                        tag?.ColorHex
                    };
                })
        ];
    }

    private static bool MatchesMapPlacement(string query, Event eventItem)
    {
        string normalizedQuery = query.Trim().ToLowerInvariant();
        string[] placedValues = ["placed", "on map", "mapped", "na mapi"];
        string[] unplacedValues = ["unplaced", "not placed", "not on map", "nije na mapi"];

        if (placedValues.Contains(normalizedQuery))
        {
            return eventItem.IsPlacedOnMap;
        }

        if (unplacedValues.Contains(normalizedQuery))
        {
            return !eventItem.IsPlacedOnMap;
        }

        return MatchesAny(query, GetMapPlacementSearchValues(eventItem));
    }

    private static string?[] GetMapPlacementSearchValues(Event eventItem)
    {
        return eventItem.IsPlacedOnMap
            ? ["placed", "on map", "mapped", "na mapi"]
            : ["unplaced", "not placed", "not on map", "nije na mapi"];
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

        bool confirmed = AppDialogService.ConfirmText(
            GetString("DeleteEventTitle"),
            string.Format(GetString("DeleteEventConfirmation"), item.Name),
            GetString("DeleteLabel"),
            GetString("CancelLabel"));

        if (!confirmed)
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

    private static string GetString(string resourceKey)
    {
        return Application.Current.TryFindResource(resourceKey) as string ?? resourceKey;
    }
}
