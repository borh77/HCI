using System.IO;
using StickItApp.Models;
using StickItApp.Services;

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

    public string Id => string.IsNullOrWhiteSpace(Event.Id) ? string.Empty : Event.Id;

    public string ShortCode => Id.Length <= 5 ? Id : Id[..5];

    public string Name => string.IsNullOrWhiteSpace(Event.Name) ? "Unnamed event" : Event.Name;

    public string Initial => string.IsNullOrWhiteSpace(Name)
        ? "?"
        : Name.Trim()[0].ToString().ToUpperInvariant();

    public string City => Event.City ?? string.Empty;

    public string Country => Event.Country ?? string.Empty;

    public string Location => string.IsNullOrWhiteSpace(City) && string.IsNullOrWhiteSpace(Country)
        ? "-"
        : $"{City}, {Country}".Trim(' ', ',');

    public DateTime CurrentStart => Event.CurrentStart == default ? Event.Date : Event.CurrentStart;

    public DateTime CurrentEnd => Event.CurrentEnd == default ? Event.Date : Event.CurrentEnd;

    public string DateText => CurrentStart == CurrentEnd
        ? CurrentStart.ToString("yyyy-MM-dd")
        : $"{CurrentStart:yyyy-MM-dd} - {CurrentEnd:yyyy-MM-dd}";

    public EventType? Type => App.DataStore.EventTypes.FirstOrDefault(type => type.Id == Event.TypeId);

    public string TypeName => Type is null ? "Unknown" : DisplayTextService.ToDisplayText(Type);

    public string TypeColorHex => string.IsNullOrWhiteSpace(Type?.ColorHex) ? "#64748B" : Type.ColorHex;

    public string IconPath
    {
        get
        {
            string eventIcon = ResolvePath(Event.IconPath);
            if (!string.IsNullOrWhiteSpace(eventIcon) && File.Exists(eventIcon))
            {
                return eventIcon;
            }

            string typeIcon = ResolvePath(Type?.IconKey);
            return !string.IsNullOrWhiteSpace(typeIcon) && File.Exists(typeIcon) ? typeIcon : string.Empty;
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

    private static string ResolvePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || Path.IsPathRooted(path))
        {
            return path ?? string.Empty;
        }

        return Path.Combine(AppContext.BaseDirectory, path);
    }
}
