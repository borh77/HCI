using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using StickItApp.Commands;
using StickItApp.Models;

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

    public ObservableCollection<Tag> Tags { get; }

    public ObservableCollection<PreviousDate> PreviousDates { get; }

    public string DateText => Event.CurrentStart == Event.CurrentEnd
        ? Event.CurrentStart.ToString("yyyy-MM-dd")
        : $"{Event.CurrentStart:yyyy-MM-dd} - {Event.CurrentEnd:yyyy-MM-dd}";

    public string IconPreviewPath
    {
        get
        {
            string eventIcon = ResolvePath(Event.IconPath ?? string.Empty);
            if (File.Exists(eventIcon))
            {
                return eventIcon;
            }

            string typeIcon = ResolvePath(Type?.IconKey ?? string.Empty);
            return File.Exists(typeIcon) ? typeIcon : string.Empty;
        }
    }

    public ICommand BackCommand { get; }

    public ICommand EditCommand { get; }

    public ICommand DeleteCommand { get; }

    private void Delete()
    {
        MessageBoxResult result = MessageBox.Show(
            string.Format(GetString("DeleteEventConfirmation"), Event.Name),
            GetString("DeleteEventTitle"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
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

    private static string ResolvePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || Path.IsPathRooted(path))
        {
            return path;
        }

        return Path.Combine(AppContext.BaseDirectory, path);
    }

    private static string GetString(string resourceKey)
    {
        return Application.Current.TryFindResource(resourceKey) as string ?? resourceKey;
    }
}
