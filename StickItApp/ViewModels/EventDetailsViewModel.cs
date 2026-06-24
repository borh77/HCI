using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using StickItApp.Commands;
using StickItApp.Models;
using StickItApp.Services;

namespace StickItApp.ViewModels;

public sealed class EventDetailsViewModel : ObservableObject
{
    private readonly Action _backToList;
    private readonly Action<Event> _editEvent;
    private readonly Action<string, bool>? _showStatus;

    public EventDetailsViewModel(Event eventItem, Action backToList, Action<Event> editEvent, Action<string, bool>? showStatus = null)
    {
        Event = eventItem;
        _backToList = backToList;
        _editEvent = editEvent;
        _showStatus = showStatus;
        Tags = new ObservableCollection<Tag>(
            App.DataStore.EventTags
                .Where(relation => relation.EventId == Event.Id)
                .Select(relation => App.DataStore.Tags.FirstOrDefault(tag => tag.Id == relation.TagId))
                .Where(tag => tag is not null)!);
        PreviousDates = new ObservableCollection<PreviousDate>(
            App.DataStore.PreviousDates.Where(item => item.EventId == Event.Id));

        BackCommand = new RelayCommand(_backToList);
        EditCommand = new RelayCommand(() => _editEvent(Event));
        DeleteCommand = new RelayCommand(Delete);
    }

    public Event Event { get; }

    public EventType? Type => App.DataStore.EventTypes.FirstOrDefault(type => type.Id == Event.TypeId);

    public string TypeDisplayName => Type is null ? "-" : DisplayTextService.ToDisplayText(Type);

    public string AttendanceDisplay => DisplayTextService.ToDisplayText(Event.Attendance);

    public ObservableCollection<Tag> Tags { get; }

    public ObservableCollection<PreviousDate> PreviousDates { get; }

    public string DateText => Event.CurrentStart == Event.CurrentEnd
        ? Event.CurrentStart.ToString("yyyy-MM-dd")
        : $"{Event.CurrentStart:yyyy-MM-dd} - {Event.CurrentEnd:yyyy-MM-dd}";

    public string IconPreviewPath
    {
        get
        {
            return HasCustomImage ? CustomImagePath : TypeIconPath;
        }
    }

    public string CustomImagePath => ImagePathService.ToImageSourcePath(Event.IconPath);

    public bool HasCustomImage => !string.IsNullOrWhiteSpace(CustomImagePath);

    public string TypeIconPath => ImagePathService.ToImageSourcePath(Type?.IconKey);

    public string TypeColorHex => Type?.ColorHex ?? "#64748B";

    public ICommand BackCommand { get; }

    public ICommand EditCommand { get; }

    public ICommand DeleteCommand { get; }

    private void Delete()
    {
        bool confirmed = AppDialogService.ConfirmText(
            GetString("DeleteEventTitle"),
            string.Format(GetString("DeleteEventConfirmation"), Event.Name),
            GetString("DeleteLabel"),
            GetString("CancelLabel"));

        if (!confirmed)
        {
            return;
        }

        App.DataStore.Events.Remove(Event);
        foreach (EventTag relation in App.DataStore.EventTags.Where(item => item.EventId == Event.Id).ToList())
        {
            App.DataStore.EventTags.Remove(relation);
        }

        foreach (PreviousDate previousDate in App.DataStore.PreviousDates.Where(item => item.EventId == Event.Id).ToList())
        {
            App.DataStore.PreviousDates.Remove(previousDate);
        }

        App.DataService.SaveAll(App.DataStore);
        _showStatus?.Invoke($"Event '{Event.Name}' deleted.", false);
        _backToList();
    }

    private static string GetString(string resourceKey)
    {
        return Application.Current.TryFindResource(resourceKey) as string ?? resourceKey;
    }
}
