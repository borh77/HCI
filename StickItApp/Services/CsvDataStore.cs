using System.Collections.ObjectModel;
using StickItApp.Models;

namespace StickItApp.Services;

public sealed class CsvDataStore
{
    public ObservableCollection<Event> Events { get; } = [];

    public ObservableCollection<EventType> EventTypes { get; } = [];

    public ObservableCollection<Tag> Tags { get; } = [];

    public ObservableCollection<EventTag> EventTags { get; } = [];

    public ObservableCollection<PreviousDate> PreviousDates { get; } = [];

    public AppSettings Settings { get; set; } = new();
}
