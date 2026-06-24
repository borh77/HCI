using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using StickItApp.Commands;
using StickItApp.Models;
using StickItApp.Services;

namespace StickItApp.ViewModels;

public sealed class TypeEditorViewModel : ObservableObject
{
    private readonly Action _backToList;
    private readonly string? _originalId;
    private string _code = string.Empty;
    private string _name = string.Empty;
    private string _description = string.Empty;
    private string _iconKey = string.Empty;
    private string _validationMessage = string.Empty;

    public TypeEditorViewModel(Action backToList, EventType? type = null)
    {
        _backToList = backToList;
        _originalId = type?.Id;

        if (type is not null)
        {
            _code = type.Id;
            _name = type.Name;
            _description = type.Description;
            _iconKey = type.IconKey;
        }

        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(_backToList);
        ChangeImageCommand = new RelayCommand(ChangeImage);
        RemoveImageCommand = new RelayCommand(() => IconKey = DefaultIconOptions.First().Path);
        AutofillExampleCommand = new RelayCommand(AutofillExample);
        SelectDefaultIconCommand = new RelayCommand(parameter =>
        {
            if (parameter is TypeIconOption option)
            {
                IconKey = option.Path;
            }
        });

        if (string.IsNullOrWhiteSpace(_iconKey))
        {
            _iconKey = DefaultIconOptions.First().Path;
        }

        RefreshIconSelection();
    }

    public string PageTitle => _originalId is null ? GetString("NewTypeLabel") : GetString("EditTypeLabel");

    public bool IsCreateMode => _originalId is null;

    public IReadOnlyList<TypeIconOption> DefaultIconOptions { get; } =
    [
        new("Music", "/Assets/Icons/EventTypes/music.png"),
        new("Sport", "/Assets/Icons/EventTypes/sport.png"),
        new("Movie", "/Assets/Icons/EventTypes/movie.png"),
        new("Charity", "/Assets/Icons/EventTypes/charity.png"),
        new("Launch", "/Assets/Icons/EventTypes/launch.png"),
        new("Education", "/Assets/Icons/EventTypes/education.png"),
        new("Food", "/Assets/Icons/EventTypes/food.png"),
        new("Conference", "/Assets/Icons/EventTypes/conference.png"),
        new("Art", "/Assets/Icons/EventTypes/art.png"),
        new("Calendar", "/Assets/Icons/EventTypes/calendar.png")
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

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public string IconKey
    {
        get => _iconKey;
        set
        {
            if (SetProperty(ref _iconKey, value))
            {
                OnPropertyChanged(nameof(IconPreviewPath));
                OnPropertyChanged(nameof(HasIconPreview));
                RefreshIconSelection();
            }
        }
    }

    public string IconPreviewPath => ImagePathService.ToImageSourcePath(IconKey);

    public bool HasIconPreview => !string.IsNullOrWhiteSpace(IconPreviewPath);

    public string ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    public ICommand SaveCommand { get; }

    public ICommand CancelCommand { get; }

    public ICommand ChangeImageCommand { get; }

    public ICommand RemoveImageCommand { get; }

    public ICommand SelectDefaultIconCommand { get; }

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

        Code = CreateUniqueCode("TECH", App.DataStore.EventTypes.Select(item => item.Id));
        Name = "Technology Conference";
        Description = "Events focused on technology, innovation, startups, robotics, software, and digital creativity.";
        IconKey = DefaultIconOptions.FirstOrDefault(option =>
            option.Path.Contains("launch", StringComparison.OrdinalIgnoreCase) ||
            option.Path.Contains("conference", StringComparison.OrdinalIgnoreCase))?.Path ??
            DefaultIconOptions.First().Path;
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
            App.DataStore.EventTypes.Add(new EventType
            {
                Id = Code,
                Name = Name.Trim(),
                Description = Description.Trim(),
                IconKey = IconKey.Trim(),
                ColorHex = "#64748B"
            });
        }
        else
        {
            EventType? type = App.DataStore.EventTypes.FirstOrDefault(item => item.Id == _originalId);
            if (type is null)
            {
                ValidationMessage = GetString("TypeMissingMessage");
                return;
            }

            if (!string.Equals(_originalId, Code, StringComparison.Ordinal))
            {
                foreach (Event eventItem in App.DataStore.Events.Where(item => item.TypeId == _originalId))
                {
                    eventItem.TypeId = Code;
                }
            }

            type.Id = Code;
            type.Name = Name.Trim();
            type.Description = Description.Trim();
            type.IconKey = IconKey.Trim();
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

        bool duplicate = App.DataStore.EventTypes.Any(item =>
            !string.Equals(item.Id, _originalId, StringComparison.Ordinal) &&
            string.Equals(item.Id, Code, StringComparison.OrdinalIgnoreCase));

        if (duplicate)
        {
            ValidationMessage = GetString("CodeUniqueMessage");
            return false;
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            ValidationMessage = GetString("NameRequiredMessage");
            return false;
        }

        if (string.IsNullOrWhiteSpace(Description))
        {
            ValidationMessage = GetString("DescriptionRequiredMessage");
            return false;
        }

        ValidationMessage = string.Empty;
        return true;
    }

    private void ChangeImage()
    {
        OpenFileDialog dialog = new()
        {
            Filter = "Image files (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png",
            Multiselect = false
        };

        if (dialog.ShowDialog() == true)
        {
            IconKey = MakeRelativeWhenPossible(dialog.FileName);
        }
    }

    private static string MakeRelativeWhenPossible(string path)
    {
        string basePath = AppContext.BaseDirectory;
        return path.StartsWith(basePath, StringComparison.OrdinalIgnoreCase)
            ? Path.GetRelativePath(basePath, path)
            : path;
    }

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
               !string.IsNullOrWhiteSpace(Name) ||
               !string.IsNullOrWhiteSpace(Description) ||
               !string.Equals(IconKey, DefaultIconOptions.First().Path, StringComparison.OrdinalIgnoreCase);
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

    private void RefreshIconSelection()
    {
        foreach (TypeIconOption option in DefaultIconOptions)
        {
            option.IsSelected = string.Equals(option.Path, IconKey, StringComparison.OrdinalIgnoreCase);
        }
    }
}

public sealed class TypeIconOption : ObservableObject
{
    private bool _isSelected;

    public TypeIconOption(string label, string path)
    {
        Label = label;
        Path = path;
    }

    public string Label { get; }

    public string Path { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
