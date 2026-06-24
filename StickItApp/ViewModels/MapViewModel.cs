using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using StickItApp.Commands;
using StickItApp.Models;
using StickItApp.Services;

namespace StickItApp.ViewModels;

public sealed class MapViewModel : ObservableObject
{
    public const double IconSize = 64;

    private readonly MainWindowViewModel _shell;
    private readonly Action<string, bool>? _showStatus;
    private string _filterText = string.Empty;
    private string _message = string.Empty;
    private MapEventViewModel? _selectedEvent;

    public MapViewModel(MainWindowViewModel shell, Action<string, bool>? showStatus = null)
    {
        _shell = shell;
        _showStatus = showStatus;

        ResetFilterCommand = new RelayCommand(ResetMapView);
        ClearFilterCommand = new RelayCommand(ClearFilter);
        ClearMessageCommand = new RelayCommand(ClearMessage);
        ClearMapCommand = new RelayCommand(ClearMap);
        AddEventCommand = new RelayCommand(() => _shell.NavigateToEventEditor(null));
        SelectEventCommand = new RelayCommand(parameter =>
        {
            if (parameter is MapEventViewModel item)
            {
                SelectedEvent = item;
            }
        });
        DetailsCommand = new RelayCommand(() => RunForSelected(_shell.NavigateToEventDetails));
        EditCommand = new RelayCommand(() => RunForSelected(_shell.NavigateToEventEditor));
        DeleteCommand = new RelayCommand(DeleteSelected);
        CloseDetailsCommand = new RelayCommand(CloseDetails);
        BackToListCommand = new RelayCommand(() =>
        {
            if (SelectedEvent is not null)
            {
                ReturnToList(SelectedEvent);
            }
        });

        Refresh();
    }

    public ObservableCollection<MapEventViewModel> MappedEvents { get; } = [];

    public ObservableCollection<MapEventViewModel> UnplacedEvents { get; } = [];

    public string FilterText
    {
        get => _filterText;
        set
        {
            if (SetProperty(ref _filterText, value))
            {
                OnPropertyChanged(nameof(HasFilterText));
                OnPropertyChanged(nameof(IsResetVisible));
                Refresh();
            }
        }
    }

    public string Message
    {
        get => _message;
        private set
        {
            if (SetProperty(ref _message, value))
            {
                OnPropertyChanged(nameof(HasMessage));
                OnPropertyChanged(nameof(IsResetVisible));
            }
        }
    }

    public MapEventViewModel? SelectedEvent
    {
        get => _selectedEvent;
        set
        {
            if (SetProperty(ref _selectedEvent, value))
            {
                OnPropertyChanged(nameof(IsResetVisible));
            }
        }
    }

    public bool HasFilterText => !string.IsNullOrWhiteSpace(FilterText);

    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);

    public bool IsResetVisible => HasFilterText || HasMessage || SelectedEvent is not null;

    public ICommand ResetFilterCommand { get; }

    public ICommand ClearFilterCommand { get; }

    public ICommand ClearMessageCommand { get; }

    public ICommand ClearMapCommand { get; }

    public ICommand AddEventCommand { get; }

    public ICommand SelectEventCommand { get; }

    public ICommand DetailsCommand { get; }

    public ICommand EditCommand { get; }

    public ICommand DeleteCommand { get; }

    public ICommand CloseDetailsCommand { get; }

    public ICommand BackToListCommand { get; }

    public bool PlaceOnMap(MapEventViewModel item, double x, double y, double mapWidth, double mapHeight)
    {
        double clampedX = Clamp(x, 0, Math.Max(0, mapWidth - IconSize));
        double clampedY = Clamp(y, 0, Math.Max(0, mapHeight - IconSize));

        if (OverlapsAnotherEvent(item.Event, clampedX, clampedY))
        {
            Message = GetString("NoOverlapMessage");
            item.SyncFromEvent();
            return false;
        }

        item.Event.X = clampedX;
        item.Event.Y = clampedY;
        item.Event.IsPlacedOnMap = true;
        item.Event.UpdatedAt = DateTime.Today;
        item.X = clampedX;
        item.Y = clampedY;
        Message = string.Empty;
        _showStatus?.Invoke(string.Empty, false);
        SaveAndRefresh(keepSelectedEventId: item.Event.Id);
        return true;
    }

    public void ReturnToList(MapEventViewModel item)
    {
        item.Event.IsPlacedOnMap = false;
        item.Event.X = 0;
        item.Event.Y = 0;
        item.Event.UpdatedAt = DateTime.Today;
        Message = string.Empty;
        _showStatus?.Invoke(string.Empty, false);
        SelectedEvent = null;
        SaveAndRefresh();
    }

    public void ResetMapView()
    {
        _filterText = string.Empty;
        OnPropertyChanged(nameof(FilterText));
        SelectedEvent = null;
        Message = string.Empty;
        _showStatus?.Invoke(string.Empty, false);
        Refresh();
    }

    private void ClearFilter()
    {
        FilterText = string.Empty;
    }

    private void ClearMessage()
    {
        Message = string.Empty;
        _showStatus?.Invoke(string.Empty, false);
    }

    private void CloseDetails()
    {
        SelectedEvent = null;
        Message = string.Empty;
        _showStatus?.Invoke(string.Empty, false);
    }

    private void Refresh()
    {
        string? selectedId = SelectedEvent?.Id;
        MappedEvents.Clear();
        UnplacedEvents.Clear();

        foreach (Event eventItem in App.DataStore.Events.OrderBy(item => item.Name ?? string.Empty))
        {
            MapEventViewModel item = new(eventItem);
            if (eventItem.IsPlacedOnMap)
            {
                if (MatchesFilter(eventItem))
                {
                    MappedEvents.Add(item);
                }
            }
            else
            {
                UnplacedEvents.Add(item);
            }
        }

        SelectedEvent = selectedId is null
            ? null
            : MappedEvents.Concat(UnplacedEvents).FirstOrDefault(item => item.Id == selectedId);
    }

    private bool MatchesFilter(Event eventItem)
    {
        if (string.IsNullOrWhiteSpace(FilterText))
        {
            return true;
        }

        string query = FilterText.Trim();
        EventType? type = App.DataStore.EventTypes.FirstOrDefault(item => item.Id == eventItem.TypeId);

        return MatchesAny(
                   query,
                   eventItem.Id,
                   eventItem.Name,
                   eventItem.City,
                   eventItem.Country,
                   GetLocationSearchValue(eventItem),
                   eventItem.Description,
                   eventItem.TypeId,
                   type?.Id,
                   type?.Name,
                   type?.Description,
                   eventItem.AverageCost.ToString(CultureInfo.CurrentCulture),
                   eventItem.AverageCost.ToString(CultureInfo.InvariantCulture)) ||
               MatchesAny(query, GetAttendanceSearchValues(eventItem)) ||
               MatchesAny(query, GetCharitableSearchValues(eventItem)) ||
               MatchesAny(query, GetDateSearchValues(eventItem)) ||
               MatchesAny(query, GetTagSearchValues(eventItem));
    }

    private static bool MatchesAny(string query, params string?[] values)
    {
        return values.Any(value => !string.IsNullOrWhiteSpace(value) &&
                                   value.Contains(query, StringComparison.OrdinalIgnoreCase));
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
            ? ["true", "yes", "charitable", "charity", "dobrotvorno", "da"]
            : ["false", "no", "ne"];
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
        values.Add(date.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture));
        values.Add(date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture));
        values.Add(date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
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

    private bool OverlapsAnotherEvent(Event movingEvent, double x, double y)
    {
        foreach (Event eventItem in App.DataStore.Events)
        {
            if (!eventItem.IsPlacedOnMap || eventItem.Id == movingEvent.Id)
            {
                continue;
            }

            bool separated = x + IconSize <= eventItem.X ||
                             eventItem.X + IconSize <= x ||
                             y + IconSize <= eventItem.Y ||
                             eventItem.Y + IconSize <= y;

            if (!separated)
            {
                return true;
            }
        }

        return false;
    }

    private void ClearMap()
    {
        if (!AppDialogService.Confirm(
                "ClearMapConfirmTitle",
                "ClearMapConfirmMessage",
                "ClearMapConfirmAction",
                "CancelLabel"))
        {
            return;
        }

        foreach (Event eventItem in App.DataStore.Events)
        {
            eventItem.IsPlacedOnMap = false;
            eventItem.X = 0;
            eventItem.Y = 0;
            eventItem.UpdatedAt = DateTime.Today;
        }

        Message = string.Empty;
        SelectedEvent = null;
        SaveAndRefresh();
    }

    private void DeleteSelected()
    {
        if (SelectedEvent is null)
        {
            return;
        }

        string eventId = SelectedEvent.Id;
        string eventName = SelectedEvent.Name;

        bool confirmed = AppDialogService.ConfirmText(
            GetString("DeleteEventTitle"),
            string.Format(GetString("DeleteEventConfirmation"), eventName),
            GetString("DeleteLabel"),
            GetString("CancelLabel"));

        if (!confirmed)
        {
            return;
        }

        App.DataStore.Events.Remove(SelectedEvent.Event);

        foreach (EventTag relation in App.DataStore.EventTags.Where(item => item.EventId == eventId).ToList())
        {
            App.DataStore.EventTags.Remove(relation);
        }

        foreach (PreviousDate previousDate in App.DataStore.PreviousDates.Where(item => item.EventId == eventId).ToList())
        {
            App.DataStore.PreviousDates.Remove(previousDate);
        }

        Message = string.Empty;
        _showStatus?.Invoke($"Event '{eventName}' deleted.", false);
        SaveAndRefresh();
    }

    private void RunForSelected(Action<Event> action)
    {
        if (SelectedEvent is not null)
        {
            action(SelectedEvent.Event);
        }
    }

    private void SaveAndRefresh(string? keepSelectedEventId = null)
    {
        App.DataService.SaveAll(App.DataStore);
        Refresh();

        if (!string.IsNullOrWhiteSpace(keepSelectedEventId))
        {
            SelectedEvent = MappedEvents.Concat(UnplacedEvents).FirstOrDefault(item => item.Id == keepSelectedEventId);
        }
    }

    private static double Clamp(double value, double min, double max)
    {
        if (value < min)
        {
            return min;
        }

        return value > max ? max : value;
    }

    private static string GetString(string resourceKey)
    {
        return Application.Current.TryFindResource(resourceKey) as string ?? resourceKey;
    }
}
