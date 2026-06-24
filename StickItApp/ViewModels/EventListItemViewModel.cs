using StickItApp.Models;
using StickItApp.Services;

namespace StickItApp.ViewModels;

public sealed class EventListItemViewModel
{
    public EventListItemViewModel(Event eventItem)
    {
        Event = eventItem;
    }

    public Event Event { get; }

    public string Id => Event.Id;

    public string Name => Event.Name;

    public string Description => Event.Description;

    public string City => Event.City;

    public string Country => Event.Country;

    public AttendanceCategory Attendance => Event.Attendance;

    public string AttendanceDisplay => DisplayTextService.ToDisplayText(Event.Attendance);

    public DateTime CurrentStart => Event.CurrentStart == default ? Event.Date : Event.CurrentStart;

    public DateTime CurrentEnd => Event.CurrentEnd == default ? Event.Date : Event.CurrentEnd;

    public bool IsPlacedOnMap => Event.IsPlacedOnMap;

    public string Location => string.IsNullOrWhiteSpace(City) && string.IsNullOrWhiteSpace(Country)
        ? "-"
        : $"{City}, {Country}".Trim(' ', ',');

    public string DateText => CurrentStart == CurrentEnd
        ? CurrentStart.ToString("yyyy-MM-dd")
        : $"{CurrentStart:yyyy-MM-dd} - {CurrentEnd:yyyy-MM-dd}";

    public EventType? Type => App.DataStore.EventTypes.FirstOrDefault(type => type.Id == Event.TypeId);

    public string CustomImagePath => ImagePathService.ToImageSourcePath(Event.IconPath);

    public bool HasCustomImage => !string.IsNullOrWhiteSpace(CustomImagePath);

    public string TypeIconPath => ImagePathService.ToImageSourcePath(Type?.IconKey);

    public string IconPath
    {
        get
        {
            return HasCustomImage ? CustomImagePath : TypeIconPath;
        }
    }

    public string TypeColorHex => Type?.ColorHex ?? "#64748B";

}
