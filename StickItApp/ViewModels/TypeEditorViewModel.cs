using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using StickItApp.Commands;
using StickItApp.Models;

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
        RemoveImageCommand = new RelayCommand(() => IconKey = string.Empty);
    }

    public string PageTitle => _originalId is null ? "New Type" : "Edit Type";

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
            }
        }
    }

    public string? IconPreviewPath
    {
        get
        {
            string path = ResolvePath(IconKey);
            return File.Exists(path) ? path : null;
        }
    }

    public string ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    public ICommand SaveCommand { get; }

    public ICommand CancelCommand { get; }

    public ICommand ChangeImageCommand { get; }

    public ICommand RemoveImageCommand { get; }

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
                ValidationMessage = "The selected type no longer exists.";
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
            ValidationMessage = "Code is required.";
            return false;
        }

        bool duplicate = App.DataStore.EventTypes.Any(item =>
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

        if (string.IsNullOrWhiteSpace(Description))
        {
            ValidationMessage = "Description is required.";
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

    private static string ResolvePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || Path.IsPathRooted(path))
        {
            return path;
        }

        return Path.Combine(AppContext.BaseDirectory, path);
    }
}
