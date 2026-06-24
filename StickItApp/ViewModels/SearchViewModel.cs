using System.Collections.ObjectModel;
using System.Windows.Input;
using StickItApp.Commands;
using StickItApp.Models;

namespace StickItApp.ViewModels;

public sealed class SearchViewModel : ObservableObject
{
    private readonly Action<Event> _showDetails;
    private readonly Action<Event> _editEvent;
    private string _eventName = string.Empty;
    private string _description = string.Empty;
    private string _tagsSearchText = string.Empty;
    private EventType? _selectedType;
    private string _selectedAttendance = "All";
    private string _locationText = string.Empty;
    private string _selectedCharitable = "All";

    public SearchViewModel(Action<Event> showDetails, Action<Event> editEvent)
    {
        _showDetails = showDetails;
        _editEvent = editEvent;

        TypeOptions.Add(new EventType { Id = string.Empty, Name = "All" });
        foreach (EventType type in App.DataStore.EventTypes)
        {
            TypeOptions.Add(type);
        }

        SelectedType = TypeOptions.FirstOrDefault();
        AttendanceOptions = ["All", .. Enum.GetValues<AttendanceCategory>().Select(item => item.ToString())];

        SearchCommand = new RelayCommand(Search);
        ResetCommand = new RelayCommand(Reset);
        DetailsCommand = new RelayCommand(parameter => RunForEvent(parameter, _showDetails));
        EditCommand = new RelayCommand(parameter => RunForEvent(parameter, _editEvent));
        ToggleMapCommand = new RelayCommand(ToggleMap);

        Search();
    }

    public ObservableCollection<EventType> TypeOptions { get; } = [];

    public IReadOnlyList<string> AttendanceOptions { get; }

    public IReadOnlyList<string> CharitableOptions { get; } = ["All", "Yes", "No"];

    public ObservableCollection<EventListItemViewModel> Results { get; } = [];

    public string EventName
    {
        get => _eventName;
        set => SetProperty(ref _eventName, value);
    }

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public string TagsSearchText
    {
        get => _tagsSearchText;
        set => SetProperty(ref _tagsSearchText, value);
    }

    public EventType? SelectedType
    {
        get => _selectedType;
        set => SetProperty(ref _selectedType, value);
    }

    public string SelectedAttendance
    {
        get => _selectedAttendance;
        set => SetProperty(ref _selectedAttendance, string.IsNullOrWhiteSpace(value) ? "All" : value);
    }

    public string LocationText
    {
        get => _locationText;
        set => SetProperty(ref _locationText, value);
    }

    public string SelectedCharitable
    {
        get => _selectedCharitable;
        set => SetProperty(ref _selectedCharitable, string.IsNullOrWhiteSpace(value) ? "All" : value);
    }

    public ICommand SearchCommand { get; }

    public ICommand ResetCommand { get; }

    public ICommand DetailsCommand { get; }

    public ICommand EditCommand { get; }

    public ICommand ToggleMapCommand { get; }

    public void Search()
    {
        Results.Clear();

        foreach (Event eventItem in App.DataStore.Events.Where(MatchesCriteria).OrderBy(item => item.Name))
        {
            Results.Add(new EventListItemViewModel(eventItem));
        }
    }

    public void Reset()
    {
        EventName = string.Empty;
        Description = string.Empty;
        TagsSearchText = string.Empty;
        SelectedType = TypeOptions.FirstOrDefault();
        SelectedAttendance = "All";
        LocationText = string.Empty;
        SelectedCharitable = "All";
        Search();
    }

    private bool MatchesCriteria(Event eventItem)
    {
        return MatchesText(eventItem.Name, EventName) &&
               MatchesText(eventItem.Description, Description) &&
               MatchesTags(eventItem, TagsSearchText) &&
               MatchesType(eventItem) &&
               MatchesAttendance(eventItem) &&
               MatchesLocation(eventItem) &&
               MatchesCharitable(eventItem);
    }

    private static bool MatchesText(string value, string query)
    {
        return string.IsNullOrWhiteSpace(query) ||
               value.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesTags(Event eventItem, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        string trimmedQuery = query.Trim();
        IEnumerable<Tag> tags = App.DataStore.EventTags
            .Where(relation => relation.EventId == eventItem.Id)
            .Select(relation => App.DataStore.Tags.FirstOrDefault(tag => tag.Id == relation.TagId))
            .OfType<Tag>();

        return tags.Any(tag =>
            tag.Id.Contains(trimmedQuery, StringComparison.OrdinalIgnoreCase) ||
            tag.Name.Contains(trimmedQuery, StringComparison.OrdinalIgnoreCase) ||
            tag.Description.Contains(trimmedQuery, StringComparison.OrdinalIgnoreCase));
    }

    private bool MatchesType(Event eventItem)
    {
        return SelectedType is null ||
               string.IsNullOrWhiteSpace(SelectedType.Id) ||
               string.Equals(eventItem.TypeId, SelectedType.Id, StringComparison.Ordinal);
    }

    private bool MatchesAttendance(Event eventItem)
    {
        return string.Equals(SelectedAttendance, "All", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(eventItem.Attendance.ToString(), SelectedAttendance, StringComparison.OrdinalIgnoreCase);
    }

    private bool MatchesLocation(Event eventItem)
    {
        if (string.IsNullOrWhiteSpace(LocationText))
        {
            return true;
        }

        string query = LocationText.Trim();
        return eventItem.City.Contains(query, StringComparison.OrdinalIgnoreCase) ||
               eventItem.Country.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private bool MatchesCharitable(Event eventItem)
    {
        return SelectedCharitable switch
        {
            "Yes" => eventItem.IsCharitable,
            "No" => !eventItem.IsCharitable,
            _ => true
        };
    }

    private static void RunForEvent(object? parameter, Action<Event> action)
    {
        if (parameter is EventListItemViewModel item)
        {
            action(item.Event);
        }
    }

    private void ToggleMap(object? parameter)
    {
        if (parameter is not EventListItemViewModel item)
        {
            return;
        }

        item.Event.IsPlacedOnMap = !item.Event.IsPlacedOnMap;
        item.Event.UpdatedAt = DateTime.Today;
        App.DataService.SaveAll(App.DataStore);
        Search();
    }
}
