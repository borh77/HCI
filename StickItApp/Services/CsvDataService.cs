using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using StickItApp.Models;

namespace StickItApp.Services;

public sealed class CsvDataService
{
    private const char Separator = ';';
    private const string DateFormat = "yyyy-MM-dd";

    private static readonly string[] EventHeader =
    [
        "Id",
        "Name",
        "City",
        "Country",
        "Description",
        "AverageCost",
        "Attendance",
        "IsCharitable",
        "TypeId",
        "IconPath",
        "CurrentStart",
        "CurrentEnd",
        "IsPlacedOnMap",
        "X",
        "Y",
        "IsCompleted",
        "CreatedAt",
        "UpdatedAt"
    ];

    private static readonly string[] EventTypeHeader = ["Id", "Name", "IconKey", "ColorHex", "Description"];
    private static readonly string[] TagHeader = ["Id", "Name", "ColorHex"];
    private static readonly string[] EventTagHeader = ["EventId", "TagId"];
    private static readonly string[] PreviousDateHeader = ["Id", "EventId", "StartDate", "EndDate"];
    private static readonly string[] SettingsHeader = ["Key", "Value"];

    public CsvDataService()
        : this(Path.Combine(AppContext.BaseDirectory, "Data"))
    {
    }

    public CsvDataService(string dataDirectory)
    {
        DataDirectory = dataDirectory;
    }

    public string DataDirectory { get; }

    private string EventsPath => Path.Combine(DataDirectory, "events.csv");

    private string EventTypesPath => Path.Combine(DataDirectory, "event_types.csv");

    private string TagsPath => Path.Combine(DataDirectory, "tags.csv");

    private string EventTagsPath => Path.Combine(DataDirectory, "event_tags.csv");

    private string PreviousDatesPath => Path.Combine(DataDirectory, "previous_dates.csv");

    private string SettingsPath => Path.Combine(DataDirectory, "settings.csv");

    public void Initialize()
    {
        EnsureFiles();

        if (!HasDataRows(EventsPath) && !HasDataRows(EventTypesPath) && !HasDataRows(TagsPath))
        {
            SaveAll(CreateSampleData());
            return;
        }

        if (!HasDataRows(SettingsPath))
        {
            SaveSettings(new AppSettings());
        }
    }

    public CsvDataStore LoadAll()
    {
        EnsureFiles();

        CsvDataStore store = new();

        LoadEventTypes(store.EventTypes);
        LoadTags(store.Tags);
        LoadEvents(store.Events, store.EventTypes);
        LoadEventTags(store.EventTags, store.Events, store.Tags);
        LoadPreviousDates(store.PreviousDates, store.Events);
        store.Settings = LoadSettings();

        return store;
    }

    public void SaveAll(CsvDataStore store)
    {
        Directory.CreateDirectory(DataDirectory);
        SaveEventTypes(store.EventTypes);
        SaveTags(store.Tags);
        SaveEvents(store.Events);
        SaveEventTags(store.EventTags);
        SavePreviousDates(store.PreviousDates);
        SaveSettings(store.Settings);
    }

    private void EnsureFiles()
    {
        Directory.CreateDirectory(DataDirectory);
        EnsureFile(EventsPath, EventHeader);
        EnsureFile(EventTypesPath, EventTypeHeader);
        EnsureFile(TagsPath, TagHeader);
        EnsureFile(EventTagsPath, EventTagHeader);
        EnsureFile(PreviousDatesPath, PreviousDateHeader);
        EnsureFile(SettingsPath, SettingsHeader);
    }

    private static void EnsureFile(string path, IReadOnlyList<string> header)
    {
        if (File.Exists(path) && new FileInfo(path).Length > 0)
        {
            return;
        }

        WriteRows(path, [header]);
    }

    private static bool HasDataRows(string path)
    {
        if (!File.Exists(path))
        {
            return false;
        }

        return ReadRows(path).Skip(1).Any(row => row.Any(field => !string.IsNullOrWhiteSpace(field)));
    }

    private void LoadEventTypes(ObservableCollection<EventType> eventTypes)
    {
        HashSet<string> seenIds = [];

        foreach (IReadOnlyList<string> row in ReadDataRows(EventTypesPath))
        {
            if (row.Count < 5 || string.IsNullOrWhiteSpace(row[0]) || !seenIds.Add(row[0]))
            {
                continue;
            }

            eventTypes.Add(new EventType
            {
                Id = row[0],
                Name = row[1],
                IconKey = row[2],
                ColorHex = row[3],
                Description = row[4]
            });
        }
    }

    private void LoadTags(ObservableCollection<Tag> tags)
    {
        HashSet<string> seenIds = [];

        foreach (IReadOnlyList<string> row in ReadDataRows(TagsPath))
        {
            if (row.Count < 3 || string.IsNullOrWhiteSpace(row[0]) || !seenIds.Add(row[0]))
            {
                continue;
            }

            tags.Add(new Tag
            {
                Id = row[0],
                Name = row[1],
                ColorHex = row[2]
            });
        }
    }

    private void LoadEvents(ObservableCollection<Event> events, ObservableCollection<EventType> eventTypes)
    {
        HashSet<string> typeIds = eventTypes.Select(type => type.Id).ToHashSet();
        HashSet<string> seenIds = [];

        foreach (IReadOnlyList<string> row in ReadDataRows(EventsPath))
        {
            if (row.Count >= 18)
            {
                LoadExtendedEventRow(events, typeIds, seenIds, row);
                continue;
            }

            if (row.Count < 10 ||
                string.IsNullOrWhiteSpace(row[0]) ||
                string.IsNullOrWhiteSpace(row[4]) ||
                !seenIds.Add(row[0]) ||
                !typeIds.Contains(row[4]))
            {
                continue;
            }

            if (!TryParseDate(row[3], out DateTime eventDate) ||
                !TryParseDouble(row[5], out double x) ||
                !TryParseDouble(row[6], out double y) ||
                !TryParseDate(row[8], out DateTime createdAt) ||
                !TryParseDate(row[9], out DateTime updatedAt))
            {
                continue;
            }

            events.Add(new Event
            {
                Id = row[0],
                Name = row[1],
                Description = row[2],
                Date = eventDate,
                CurrentStart = eventDate,
                CurrentEnd = eventDate,
                TypeId = row[4],
                X = x,
                Y = y,
                IsPlacedOnMap = true,
                IsCompleted = ParseBool(row[7]),
                CreatedAt = createdAt,
                UpdatedAt = updatedAt
            });
        }
    }

    private static void LoadExtendedEventRow(
        ObservableCollection<Event> events,
        HashSet<string> typeIds,
        HashSet<string> seenIds,
        IReadOnlyList<string> row)
    {
        if (string.IsNullOrWhiteSpace(row[0]) ||
            string.IsNullOrWhiteSpace(row[8]) ||
            !seenIds.Add(row[0]) ||
            !typeIds.Contains(row[8]))
        {
            return;
        }

        if (!TryParseDecimal(row[5], out decimal averageCost) ||
            !TryParseDate(row[10], out DateTime currentStart) ||
            !TryParseDate(row[11], out DateTime currentEnd) ||
            !TryParseDouble(row[13], out double x) ||
            !TryParseDouble(row[14], out double y) ||
            !TryParseDate(row[16], out DateTime createdAt) ||
            !TryParseDate(row[17], out DateTime updatedAt))
        {
            return;
        }

        events.Add(new Event
        {
            Id = row[0],
            Name = row[1],
            City = row[2],
            Country = row[3],
            Description = row[4],
            AverageCost = averageCost,
            Attendance = ParseAttendance(row[6]),
            IsCharitable = ParseBool(row[7]),
            TypeId = row[8],
            IconPath = string.IsNullOrWhiteSpace(row[9]) ? null : row[9],
            Date = currentStart,
            CurrentStart = currentStart,
            CurrentEnd = currentEnd,
            IsPlacedOnMap = ParseBool(row[12]),
            X = x,
            Y = y,
            IsCompleted = ParseBool(row[15]),
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        });
    }

    private void LoadEventTags(
        ObservableCollection<EventTag> eventTags,
        ObservableCollection<Event> events,
        ObservableCollection<Tag> tags)
    {
        HashSet<string> eventIds = events.Select(item => item.Id).ToHashSet();
        HashSet<string> tagIds = tags.Select(item => item.Id).ToHashSet();
        HashSet<string> seenPairs = [];

        foreach (IReadOnlyList<string> row in ReadDataRows(EventTagsPath))
        {
            if (row.Count < 2 ||
                string.IsNullOrWhiteSpace(row[0]) ||
                string.IsNullOrWhiteSpace(row[1]) ||
                !eventIds.Contains(row[0]) ||
                !tagIds.Contains(row[1]) ||
                !seenPairs.Add($"{row[0]}\u001F{row[1]}"))
            {
                continue;
            }

            eventTags.Add(new EventTag
            {
                EventId = row[0],
                TagId = row[1]
            });
        }
    }

    private void LoadPreviousDates(ObservableCollection<PreviousDate> previousDates, ObservableCollection<Event> events)
    {
        HashSet<string> eventIds = events.Select(item => item.Id).ToHashSet();
        HashSet<string> seenIds = [];

        foreach (IReadOnlyList<string> row in ReadDataRows(PreviousDatesPath))
        {
            if (row.Count < 3 ||
                string.IsNullOrWhiteSpace(row[0]) ||
                string.IsNullOrWhiteSpace(row[1]) ||
                !seenIds.Add(row[0]) ||
                !eventIds.Contains(row[1]))
            {
                continue;
            }

            DateTime start;
            DateTime end;
            if (row.Count >= 4)
            {
                if (!TryParseDate(row[2], out start) || !TryParseDate(row[3], out end))
                {
                    continue;
                }
            }
            else if (TryParseDate(row[2], out DateTime singleDate))
            {
                start = singleDate;
                end = singleDate;
            }
            else
            {
                continue;
            }

            previousDates.Add(new PreviousDate
            {
                Id = row[0],
                EventId = row[1],
                Date = start,
                Start = start,
                End = end
            });
        }
    }

    private AppSettings LoadSettings()
    {
        AppSettings settings = new();

        foreach (IReadOnlyList<string> row in ReadDataRows(SettingsPath))
        {
            if (row.Count < 2)
            {
                continue;
            }

            switch (row[0])
            {
                case nameof(AppSettings.Language):
                    settings.Language = string.IsNullOrWhiteSpace(row[1]) ? settings.Language : row[1];
                    break;
                case nameof(AppSettings.Theme):
                    settings.Theme = string.IsNullOrWhiteSpace(row[1]) ? settings.Theme : row[1];
                    break;
                case nameof(AppSettings.LastSortMode):
                    settings.LastSortMode = string.IsNullOrWhiteSpace(row[1]) ? settings.LastSortMode : row[1];
                    break;
                case nameof(AppSettings.LastSearchText):
                    settings.LastSearchText = row[1];
                    break;
            }
        }

        return settings;
    }

    private void SaveEvents(IEnumerable<Event> events)
    {
        List<IReadOnlyList<string>> rows = [EventHeader];

        rows.AddRange(events.Select(item => new[]
        {
            item.Id,
            item.Name,
            item.City,
            item.Country,
            item.Description,
            FormatDecimal(item.AverageCost),
            item.Attendance.ToString(),
            FormatBool(item.IsCharitable),
            item.TypeId,
            item.IconPath ?? string.Empty,
            FormatDate(item.CurrentStart == default ? item.Date : item.CurrentStart),
            FormatDate(item.CurrentEnd == default ? item.Date : item.CurrentEnd),
            FormatBool(item.IsPlacedOnMap),
            FormatDouble(item.X),
            FormatDouble(item.Y),
            FormatBool(item.IsCompleted),
            FormatDate(item.CreatedAt),
            FormatDate(item.UpdatedAt)
        }));

        WriteRows(EventsPath, rows);
    }

    private void SaveEventTypes(IEnumerable<EventType> eventTypes)
    {
        List<IReadOnlyList<string>> rows = [EventTypeHeader];
        rows.AddRange(eventTypes.Select(item => new[]
        {
            item.Id,
            item.Name,
            item.IconKey,
            item.ColorHex,
            item.Description
        }));
        WriteRows(EventTypesPath, rows);
    }

    private void SaveTags(IEnumerable<Tag> tags)
    {
        List<IReadOnlyList<string>> rows = [TagHeader];
        rows.AddRange(tags.Select(item => new[] { item.Id, item.Name, item.ColorHex }));
        WriteRows(TagsPath, rows);
    }

    private void SaveEventTags(IEnumerable<EventTag> eventTags)
    {
        List<IReadOnlyList<string>> rows = [EventTagHeader];
        rows.AddRange(eventTags.Select(item => new[] { item.EventId, item.TagId }));
        WriteRows(EventTagsPath, rows);
    }

    private void SavePreviousDates(IEnumerable<PreviousDate> previousDates)
    {
        List<IReadOnlyList<string>> rows = [PreviousDateHeader];
        rows.AddRange(previousDates.Select(item => new[]
        {
            item.Id,
            item.EventId,
            FormatDate(item.Start == default ? item.Date : item.Start),
            FormatDate(item.End == default ? item.Date : item.End)
        }));
        WriteRows(PreviousDatesPath, rows);
    }

    public void SaveSettings(AppSettings settings)
    {
        WriteRows(SettingsPath,
        [
            SettingsHeader,
            [nameof(AppSettings.Language), settings.Language],
            [nameof(AppSettings.Theme), settings.Theme],
            [nameof(AppSettings.LastSortMode), settings.LastSortMode],
            [nameof(AppSettings.LastSearchText), settings.LastSearchText]
        ]);
    }

    private static IEnumerable<IReadOnlyList<string>> ReadDataRows(string path)
    {
        return ReadRows(path).Skip(1);
    }

    private static List<IReadOnlyList<string>> ReadRows(string path)
    {
        if (!File.Exists(path))
        {
            return [];
        }

        string content = File.ReadAllText(path, Encoding.UTF8);
        List<IReadOnlyList<string>> rows = [];
        List<string> row = [];
        StringBuilder field = new();
        bool inQuotes = false;

        for (int i = 0; i < content.Length; i++)
        {
            char current = content[i];

            if (inQuotes)
            {
                if (current == '"')
                {
                    if (i + 1 < content.Length && content[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    field.Append(current);
                }

                continue;
            }

            if (current == '"')
            {
                inQuotes = true;
            }
            else if (current == Separator)
            {
                row.Add(field.ToString());
                field.Clear();
            }
            else if (current == '\r' || current == '\n')
            {
                if (current == '\r' && i + 1 < content.Length && content[i + 1] == '\n')
                {
                    i++;
                }

                row.Add(field.ToString());
                field.Clear();
                rows.Add(row);
                row = [];
            }
            else
            {
                field.Append(current);
            }
        }

        if (field.Length > 0 || row.Count > 0)
        {
            row.Add(field.ToString());
            rows.Add(row);
        }

        return rows;
    }

    private static void WriteRows(string path, IEnumerable<IReadOnlyList<string>> rows)
    {
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        StringBuilder builder = new();
        foreach (IReadOnlyList<string> row in rows)
        {
            builder.AppendLine(string.Join(Separator, row.Select(EscapeField)));
        }

        File.WriteAllText(path, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static string EscapeField(string? value)
    {
        string field = value ?? string.Empty;
        bool mustQuote = field.Contains(Separator) ||
                         field.Contains('"') ||
                         field.Contains('\r') ||
                         field.Contains('\n');

        if (!mustQuote)
        {
            return field;
        }

        return $"\"{field.Replace("\"", "\"\"")}\"";
    }

    private static bool TryParseDate(string value, out DateTime date)
    {
        return DateTime.TryParseExact(value, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }

    private static string FormatDate(DateTime date)
    {
        return date.ToString(DateFormat, CultureInfo.InvariantCulture);
    }

    private static bool TryParseDouble(string value, out double result)
    {
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
    }

    private static string FormatDouble(double value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static bool TryParseDecimal(string value, out decimal result)
    {
        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result);
    }

    private static string FormatDecimal(decimal value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static AttendanceCategory ParseAttendance(string value)
    {
        return Enum.TryParse(value, out AttendanceCategory attendance) ? attendance : AttendanceCategory.UpTo1000;
    }

    private static bool ParseBool(string value)
    {
        return bool.TryParse(value, out bool result) && result;
    }

    private static string FormatBool(bool value)
    {
        return value ? bool.TrueString : bool.FalseString;
    }

    private static CsvDataStore CreateSampleData()
    {
        CsvDataStore store = new();

        EventType reminder = new()
        {
            Id = "TYPE-REMINDER",
            Name = "Reminder",
            IconKey = "bell",
            ColorHex = "#2563EB",
            Description = "Small tasks and personal reminders."
        };
        EventType birthday = new()
        {
            Id = "TYPE-BIRTHDAY",
            Name = "Birthday",
            IconKey = "cake",
            ColorHex = "#DB2777",
            Description = "Birthday events and celebrations."
        };
        EventType meeting = new()
        {
            Id = "TYPE-MEETING",
            Name = "Meeting",
            IconKey = "users",
            ColorHex = "#059669",
            Description = "Group meetings and appointments."
        };
        EventType exam = new()
        {
            Id = "TYPE-EXAM",
            Name = "Exam",
            IconKey = "book-open",
            ColorHex = "#D97706",
            Description = "Exam dates and study deadlines."
        };
        EventType personal = new()
        {
            Id = "TYPE-PERSONAL",
            Name = "Personal",
            IconKey = "sticky-note",
            ColorHex = "#7C3AED",
            Description = "Personal notes on the map."
        };

        store.EventTypes.Add(reminder);
        store.EventTypes.Add(birthday);
        store.EventTypes.Add(meeting);
        store.EventTypes.Add(exam);
        store.EventTypes.Add(personal);

        Tag important = new() { Id = "TAG-IMPORTANT", Name = "Important", ColorHex = "#EF4444" };
        Tag school = new() { Id = "TAG-SCHOOL", Name = "School", ColorHex = "#F59E0B" };
        Tag family = new() { Id = "TAG-FAMILY", Name = "Family", ColorHex = "#EC4899" };
        Tag work = new() { Id = "TAG-WORK", Name = "Work", ColorHex = "#10B981" };
        Tag urgent = new() { Id = "TAG-URGENT", Name = "Urgent", ColorHex = "#DC2626" };

        store.Tags.Add(important);
        store.Tags.Add(school);
        store.Tags.Add(family);
        store.Tags.Add(work);
        store.Tags.Add(urgent);

        DateTime created = new(2026, 1, 10);

        AddSampleEvent(
            store,
            id: "EVT-HCI-EXAM",
            name: "Study for HCI exam",
            description: "Review lectures, wireframes, and WPF interaction patterns.",
            date: new DateTime(2026, 2, 5),
            typeId: exam.Id,
            x: 92,
            y: 146,
            isCompleted: false,
            createdAt: created,
            updatedAt: created,
            tagIds: [important.Id, school.Id],
            previousDates: [new DateTime(2026, 1, 29)]);

        AddSampleEvent(
            store,
            id: "EVT-DOCS",
            name: "Submit project documentation",
            description: "Prepare the final project notes and screenshots.",
            date: new DateTime(2026, 2, 12),
            typeId: reminder.Id,
            x: 244,
            y: 108,
            isCompleted: false,
            createdAt: created,
            updatedAt: created,
            tagIds: [important.Id, school.Id, urgent.Id],
            previousDates: [new DateTime(2026, 2, 8)]);

        AddSampleEvent(
            store,
            id: "EVT-BIRTHDAY",
            name: "Family birthday dinner",
            description: "Reserve a table and bring the gift.",
            date: new DateTime(2026, 3, 4),
            typeId: birthday.Id,
            x: 171,
            y: 298,
            isCompleted: false,
            createdAt: created,
            updatedAt: created,
            tagIds: [family.Id],
            previousDates: [new DateTime(2025, 3, 4)]);

        AddSampleEvent(
            store,
            id: "EVT-MEETING",
            name: "Team meeting",
            description: "Discuss prototype feedback and next steps.",
            date: new DateTime(2026, 1, 22),
            typeId: meeting.Id,
            x: 304,
            y: 242,
            isCompleted: false,
            createdAt: created,
            updatedAt: created,
            tagIds: [work.Id],
            previousDates: [new DateTime(2026, 1, 15)]);

        AddSampleEvent(
            store,
            id: "EVT-STICKY-NOTES",
            name: "Buy sticky notes",
            description: "Pick colors for planning and event grouping.",
            date: new DateTime(2026, 1, 18),
            typeId: personal.Id,
            x: 120,
            y: 415,
            isCompleted: true,
            createdAt: created,
            updatedAt: new DateTime(2026, 1, 18),
            tagIds: [],
            previousDates: []);

        store.Settings = new AppSettings
        {
            Language = "EN",
            Theme = "Light",
            LastSortMode = "Date",
            LastSearchText = string.Empty
        };

        return store;
    }

    private static void AddSampleEvent(
        CsvDataStore store,
        string id,
        string name,
        string description,
        DateTime date,
        string typeId,
        double x,
        double y,
        bool isCompleted,
        DateTime createdAt,
        DateTime updatedAt,
        IReadOnlyList<string> tagIds,
        IReadOnlyList<DateTime> previousDates)
    {
        store.Events.Add(new Event
        {
            Id = id,
            Name = name,
            Description = description,
            Date = date,
            TypeId = typeId,
            X = x,
            Y = y,
            IsCompleted = isCompleted,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        });

        foreach (string tagId in tagIds)
        {
            store.EventTags.Add(new EventTag { EventId = id, TagId = tagId });
        }

        int index = 1;
        foreach (DateTime previousDate in previousDates)
        {
            store.PreviousDates.Add(new PreviousDate
            {
                Id = $"{id}-PREV-{index}",
                EventId = id,
                Date = previousDate
            });
            index++;
        }
    }
}
