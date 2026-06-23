namespace StickItApp.Models;

public sealed class AppSettings
{
    public string Language { get; set; } = "EN";

    public string Theme { get; set; } = "Light";

    public string LastSortMode { get; set; } = "Date";

    public string LastSearchText { get; set; } = string.Empty;
}
