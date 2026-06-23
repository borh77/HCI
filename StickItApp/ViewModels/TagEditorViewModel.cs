using System.Text.RegularExpressions;
using System.Windows.Input;
using StickItApp.Commands;
using StickItApp.Models;

namespace StickItApp.ViewModels;

public sealed partial class TagEditorViewModel : ObservableObject
{
    private readonly Action _backToList;
    private readonly string? _originalId;
    private string _code = string.Empty;
    private string _name = string.Empty;
    private string _colorHex = "#FFB300";
    private string _validationMessage = string.Empty;

    public TagEditorViewModel(Action backToList, Tag? tag = null)
    {
        _backToList = backToList;
        _originalId = tag?.Id;

        if (tag is not null)
        {
            _code = tag.Id;
            _name = tag.Name;
            _colorHex = tag.ColorHex;
        }

        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(_backToList);
        SelectColorCommand = new RelayCommand(parameter =>
        {
            if (parameter is string color)
            {
                ColorHex = color;
            }
        });
    }

    public string PageTitle => _originalId is null ? "New Tag" : "Edit Tag";

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
        set => SetProperty(ref _code, value.Trim());
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string ColorHex
    {
        get => _colorHex;
        set => SetProperty(ref _colorHex, value.Trim());
    }

    public string ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    public ICommand SaveCommand { get; }

    public ICommand CancelCommand { get; }

    public ICommand SelectColorCommand { get; }

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
                Name = Name.Trim(),
                ColorHex = ColorHex
            });
        }
        else
        {
            Tag? tag = App.DataStore.Tags.FirstOrDefault(item => item.Id == _originalId);
            if (tag is null)
            {
                ValidationMessage = "The selected tag no longer exists.";
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
            tag.Name = Name.Trim();
            tag.ColorHex = ColorHex;
        }

        App.DataService.SaveAll(App.DataStore);
        _backToList();
    }

    private bool Validate()
    {
        if (string.IsNullOrWhiteSpace(Code))
        {
            ValidationMessage = "Code is required.";
            return false;
        }

        bool duplicate = App.DataStore.Tags.Any(item =>
            !string.Equals(item.Id, _originalId, StringComparison.Ordinal) &&
            string.Equals(item.Id, Code, StringComparison.OrdinalIgnoreCase));

        if (duplicate)
        {
            ValidationMessage = "Code must be unique.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            ValidationMessage = "Name is required.";
            return false;
        }

        if (!HexColorRegex().IsMatch(ColorHex))
        {
            ValidationMessage = "ColorHex must be a valid color, for example #FFB300.";
            return false;
        }

        ValidationMessage = string.Empty;
        return true;
    }

    [GeneratedRegex("^#[0-9A-Fa-f]{6}$")]
    private static partial Regex HexColorRegex();
}
