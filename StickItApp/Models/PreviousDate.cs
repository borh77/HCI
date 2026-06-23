namespace StickItApp.Models;

public sealed class PreviousDate
{
    public string Id { get; set; } = string.Empty;

    public string EventId { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    public DateTime Start { get; set; }

    public DateTime End { get; set; }
}
