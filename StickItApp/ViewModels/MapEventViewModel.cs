using System.IO;
using StickItApp.Models;

namespace StickItApp.ViewModels;

public sealed class MapEventViewModel : ObservableObject
{
    private double _x;
    private double _y;

    public MapEventViewModel(Event eventItem)
    {
        Event = eventItem;
        _x = eventItem.X;
        _y = eventItem.Y;
    }

    public Event Event { get; }

    public string Id => Event.Id;

    public string ShortCode => Id.Length <= 5 ? Id : Id[..5];

    public string Name => Event.Name;

    public string City => Event.City;

    public string Country => Event.Country;

    public string Location => string.IsNullOrWhiteSpace(City) && string.IsNullOrWhiteSpace(Country)
        ? "-"
        : $"{City}, {Country}".Trim(' ', ',');

    public DateTime CurrentStart => Event.CurrentStart == default ? Event.Date : Event.CurrentStart;

    public DateTime CurrentEnd => Event.CurrentEnd == default ? Event.Date : Event.CurrentEnd;

    public string DateText => CurrentStart == CurrentEnd
        ? CurrentStart.ToString("yyyy-MM-dd")
        : $"{CurrentStart:yyyy-MM-dd} - {CurrentEnd:yyyy-MM-dd}";

    public EventType? Type => App.DataStore.EventTypes.FirstOrDefault(type => type.Id == Event.TypeId);

    public string TypeName => Type?.Name ?? "-";

    public string TypeColorHex => Type?.ColorHex ?? "#64748B";

    public string IconPath
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

    public double X
    {
        get => _x;
        set => SetProperty(ref _x, value);
    }

    public double Y
    {
        get => _y;
        set => SetProperty(ref _y, value);
    }

    public void SyncFromEvent()
    {
        X = Event.X;
        Y = Event.Y;
    }

    private static string ResolvePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || Path.IsPathRooted(path))
        {
            return path;
        }

        return Path.Combine(AppContext.BaseDirectory, path);
    }
}
