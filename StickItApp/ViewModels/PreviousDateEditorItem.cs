namespace StickItApp.ViewModels;

public sealed class PreviousDateEditorItem
{
    public DateTime Start { get; set; }

    public DateTime End { get; set; }

    public string Text => Start == End ? Start.ToString("yyyy-MM-dd") : $"{Start:yyyy-MM-dd} - {End:yyyy-MM-dd}";
}
