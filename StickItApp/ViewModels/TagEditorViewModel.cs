using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using StickItApp.Commands;
using StickItApp.Models;
using StickItApp.Services;

namespace StickItApp.ViewModels;

public sealed partial class TagEditorViewModel : ObservableObject
{
    private readonly Action _backToList;
    private readonly string? _originalId;
    private string _code = string.Empty;
    private string _description = string.Empty;
    private string _colorHex = "#FFB300";
    private string _validationMessage = string.Empty;

    public TagEditorViewModel(Action backToList, Tag? tag = null)
    {
        _backToList = backToList;
        _originalId = tag?.Id;

        if (tag is not null)
        {
            _code = tag.Id;
            _description = tag.Description;
            _colorHex = tag.ColorHex;
        }

        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(_backToList);
        AutofillExampleCommand = new RelayCommand(AutofillExample);
        SelectColorCommand = new RelayCommand(parameter =>
        {
            if (parameter is string color)
            {
                ColorHex = color;
            }
        });
    }

    public string PageTitle => _originalId is null ? GetString("NewTagLabel") : GetString("EditTagLabel");

    public bool IsCreateMode => _originalId is null;

    public IReadOnlyList<string> PresetColors { get; } =
    [
        "#EF4444",
        "#F59E0B",
        "#10B981",
        "#3B82F6",
        "#8B5CF6",
        "#EC4899"
    ];

    public string Code
    {
        get => _code;
        set
        {
            if (SetProperty(ref _code, value.Trim()))
            {
                OnPropertyChanged(nameof(Name));
            }
        }
    }

    public string Name
    {
        get => Code;
        set => Code = value;
    }

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public string ColorHex
    {
        get => _colorHex;
        set
        {
            if (SetProperty(ref _colorHex, value.Trim()))
            {
                OnPropertyChanged(nameof(SelectedColor));
            }
        }
    }

    public Color? SelectedColor
    {
        get => TryParseColor(ColorHex);
        set
        {
            if (value is Color color)
            {
                ColorHex = ToHex(color);
            }
        }
    }

    public string ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    public ICommand SaveCommand { get; }

    public ICommand CancelCommand { get; }

    public ICommand SelectColorCommand { get; }

    public ICommand AutofillExampleCommand { get; }

    private void AutofillExample()
    {
        if (!IsCreateMode)
        {
            return;
        }

        if (HasFormData() && !ConfirmReplace())
        {
            return;
        }

        Code = CreateUniqueCode("INNOVATION", App.DataStore.Tags.Select(item => item.Id));
        Description = "Used for events focused on new ideas, technology, creativity, and modern solutions.";
        ColorHex = "#8B5CF6";
        ValidationMessage = GetString("ExampleDataFilledMessage");
    }

    private void Save()
    {
        if (!Validate())
        {
            return;
        }

        if (_originalId is null)
        {
            App.DataStore.Tags.Add(new Tag
            {
                Id = Code,
                Name = Code,
                Description = Description.Trim(),
                ColorHex = ColorHex.ToUpperInvariant()
            });
        }
        else
        {
            Tag? tag = App.DataStore.Tags.FirstOrDefault(item => item.Id == _originalId);
            if (tag is null)
            {
                ValidationMessage = GetString("TagMissingMessage");
                return;
            }

            if (!string.Equals(_originalId, Code, StringComparison.Ordinal))
            {
                foreach (EventTag relation in App.DataStore.EventTags.Where(item => item.TagId == _originalId))
                {
                    relation.TagId = Code;
                }
            }

            tag.Id = Code;
            tag.Name = Code;
            tag.Description = Description.Trim();
            tag.ColorHex = ColorHex.ToUpperInvariant();
        }

        App.DataService.SaveAll(App.DataStore);
        _backToList();
    }

    private bool Validate()
    {
        if (string.IsNullOrWhiteSpace(Code))
        {
            ValidationMessage = GetString("CodeRequiredMessage");
            return false;
        }

        bool duplicate = App.DataStore.Tags.Any(item =>
            !string.Equals(item.Id, _originalId, StringComparison.Ordinal) &&
            string.Equals(item.Id, Code, StringComparison.OrdinalIgnoreCase));

        if (duplicate)
        {
            ValidationMessage = GetString("CodeUniqueMessage");
            return false;
        }

        if (string.IsNullOrWhiteSpace(Description))
        {
            ValidationMessage = GetString("DescriptionRequiredMessage");
            return false;
        }

        if (!HexColorRegex().IsMatch(ColorHex))
        {
            ValidationMessage = GetString("ColorHexInvalidMessage");
            return false;
        }

        ValidationMessage = string.Empty;
        return true;
    }

    [GeneratedRegex("^#[0-9A-Fa-f]{6}$")]
    private static partial Regex HexColorRegex();

    private static string GetString(string resourceKey)
    {
        return Application.Current.TryFindResource(resourceKey) as string ?? resourceKey;
    }

    private static string CreateUniqueCode(string baseCode, IEnumerable<string> existingCodes)
    {
        HashSet<string> existing = existingCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!existing.Contains(baseCode))
        {
            return baseCode;
        }

        int suffix = 2;
        while (existing.Contains($"{baseCode}-{suffix}"))
        {
            suffix++;
        }

        return $"{baseCode}-{suffix}";
    }

    private bool HasFormData()
    {
        return !string.IsNullOrWhiteSpace(Code) ||
               !string.IsNullOrWhiteSpace(Description) ||
               !string.Equals(ColorHex, "#FFB300", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ConfirmReplace()
    {
        return AppDialogService.ConfirmText(
            GetString("AutofillExampleLabel"),
            GetString("AutofillReplaceConfirmation"),
            GetString("AutofillExampleLabel"),
            GetString("CancelLabel"),
            DialogKind.Info);
    }

    private static Color? TryParseColor(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        try
        {
            return (Color?)ColorConverter.ConvertFromString(value);
        }
        catch (FormatException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static string ToHex(Color color)
    {
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }
}
