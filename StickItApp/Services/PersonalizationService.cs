using System.Windows;
using StickItApp.Models;

namespace StickItApp.Services;

public static class PersonalizationService
{
    private const string ThemeResourcePath = "Resources/Themes/";
    private const string LanguageResourcePath = "Resources/Languages/";

    public static void Apply(AppSettings settings)
    {
        ApplyTheme(settings.Theme);
        ApplyLanguage(settings.Language);
    }

    public static void ApplyTheme(string theme)
    {
        string normalizedTheme = NormalizeTheme(theme);
        string fileName = normalizedTheme == "Dark" ? "DarkTheme.xaml" : "LightTheme.xaml";
        ReplaceDictionary(ThemeResourcePath, $"{ThemeResourcePath}{fileName}");
    }

    public static void ApplyLanguage(string language)
    {
        string normalizedLanguage = NormalizeLanguage(language);
        string fileName = normalizedLanguage == "SR" ? "Strings.sr.xaml" : "Strings.en.xaml";
        ReplaceDictionary(LanguageResourcePath, $"{LanguageResourcePath}{fileName}");
    }

    public static string NormalizeTheme(string theme)
    {
        return string.Equals(theme, "Dark", StringComparison.OrdinalIgnoreCase) ? "Dark" : "Light";
    }

    public static string NormalizeLanguage(string language)
    {
        return string.Equals(language, "SR", StringComparison.OrdinalIgnoreCase) ? "SR" : "EN";
    }

    private static void ReplaceDictionary(string resourcePath, string source)
    {
        var dictionaries = Application.Current.Resources.MergedDictionaries;

        for (int i = dictionaries.Count - 1; i >= 0; i--)
        {
            string? dictionarySource = dictionaries[i].Source?.OriginalString;
            if (dictionarySource?.Contains(resourcePath, StringComparison.OrdinalIgnoreCase) == true)
            {
                dictionaries.RemoveAt(i);
            }
        }

        dictionaries.Add(new ResourceDictionary
        {
            Source = new Uri(source, UriKind.Relative)
        });
    }
}
