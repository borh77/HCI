namespace StickItApp.Models;

public sealed class Event
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    public DateTime CurrentStart { get; set; }

    public DateTime CurrentEnd { get; set; }

    public decimal AverageCost { get; set; }

    public AttendanceCategory Attendance { get; set; }

    public bool IsCharitable { get; set; }

    public string TypeId { get; set; } = string.Empty;

    public string? IconPath { get; set; }

    public double X { get; set; }

    public double Y { get; set; }

    public bool IsPlacedOnMap { get; set; }

    public bool IsCompleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
