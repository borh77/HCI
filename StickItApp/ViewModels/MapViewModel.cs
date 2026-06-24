using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using StickItApp.Commands;
using StickItApp.Models;

namespace StickItApp.ViewModels;

public sealed class MapViewModel : ObservableObject
{
    public const double IconSize = 48;

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
        ClearMapCommand = new RelayCommand(ClearMap);
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
                Refresh();
            }
        }
    }

    public string Message
    {
        get => _message;
        private set => SetProperty(ref _message, value);
    }

    public MapEventViewModel? SelectedEvent
    {
        get => _selectedEvent;
        set => SetProperty(ref _selectedEvent, value);
    }

    public ICommand ResetFilterCommand { get; }

    public ICommand ClearMapCommand { get; }

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
            _showStatus?.Invoke(Message, true);
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

        foreach (Event eventItem in App.DataStore.Events.Where(MatchesFilter).OrderBy(item => item.Name ?? string.Empty))
        {
            MapEventViewModel item = new(eventItem);
            if (eventItem.IsPlacedOnMap)
            {
                MappedEvents.Add(item);
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
        return (eventItem.Id ?? string.Empty).Contains(query, StringComparison.OrdinalIgnoreCase) ||
               (eventItem.Name ?? string.Empty).Contains(query, StringComparison.OrdinalIgnoreCase);
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
        MessageBoxResult result = MessageBox.Show(
            GetString("ClearMapConfirmation"),
            GetString("ClearMapLabel"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
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

        MessageBoxResult result = MessageBox.Show(
            string.Format(GetString("DeleteEventConfirmation"), eventName),
            GetString("DeleteEventTitle"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
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
