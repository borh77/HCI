using System.Windows;
using StickItApp.Models;

namespace StickItApp.Services;

public static class DisplayTextService
{
    public static string ToDisplayText(object? value)
    {
        return value switch
        {
            null => string.Empty,
            EventType type when string.IsNullOrWhiteSpace(type.Id) && string.Equals(type.Name, "All", StringComparison.OrdinalIgnoreCase) =>
                ResourceOrFallback("AllLabel", "All"),
            EventType type => ResourceOrFallback($"Demo_{NormalizeKey(type.Name)}", type.Name),
            Tag tag => string.IsNullOrWhiteSpace(tag.Id) ? tag.Name : tag.Id,
            AttendanceCategory attendance => AttendanceToDisplayText(attendance),
            string text => StringToDisplayText(text),
            _ => value.ToString() ?? string.Empty
        };
    }

    private static string AttendanceToDisplayText(AttendanceCategory attendance)
    {
        return attendance switch
        {
            AttendanceCategory.UpTo1000 => ResourceOrFallback("Demo_AttendanceUpTo1000", "<= 1000"),
            AttendanceCategory.From1000To5000 => ResourceOrFallback("Demo_AttendanceFrom1000To5000", "1000-5000"),
            AttendanceCategory.From5000To10000 => ResourceOrFallback("Demo_AttendanceFrom5000To10000", "5000-10000"),
            AttendanceCategory.Over10000 => ResourceOrFallback("Demo_AttendanceOver10000", "> 10000"),
            _ => attendance.ToString()
        };
    }

    private static string StringToDisplayText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        return text switch
        {
            "All" => ResourceOrFallback("AllLabel", "All"),
            "Yes" => ResourceOrFallback("YesLabel", "Yes"),
            "No" => ResourceOrFallback("NoLabel", "No"),
            "Name" => ResourceOrFallback("NameLabel", "Name"),
            "Code" => ResourceOrFallback("CodeLabel", "Code"),
            "Description" => ResourceOrFallback("DescriptionLabel", "Description"),
            "City" => ResourceOrFallback("CityLabel", "City"),
            "Attendance" => ResourceOrFallback("AttendanceLabel", "Attendance"),
            nameof(AttendanceCategory.UpTo1000) => ResourceOrFallback("Demo_AttendanceUpTo1000", "<= 1000"),
            nameof(AttendanceCategory.From1000To5000) => ResourceOrFallback("Demo_AttendanceFrom1000To5000", "1000-5000"),
            nameof(AttendanceCategory.From5000To10000) => ResourceOrFallback("Demo_AttendanceFrom5000To10000", "5000-10000"),
            nameof(AttendanceCategory.Over10000) => ResourceOrFallback("Demo_AttendanceOver10000", "> 10000"),
            _ => ResourceOrFallback($"Demo_{NormalizeKey(text)}", text)
        };
    }

    private static string ResourceOrFallback(string key, string fallback)
    {
        return Application.Current?.TryFindResource(key) as string ?? fallback;
    }

    private static string NormalizeKey(string value)
    {
        return value.Replace(" ", string.Empty).Replace("-", string.Empty);
    }
}
