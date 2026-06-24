using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.Win32;
using StickItApp.Commands;
using StickItApp.Models;
using StickItApp.Services;

namespace StickItApp.ViewModels;

public sealed class EventEditorViewModel : ObservableObject
{
    private readonly Action _backToList;
    private readonly Action _addType;
    private readonly Action _addTag;
    private readonly string? _originalId;
    private string _code = string.Empty;
    private string _name = string.Empty;
    private string _city = string.Empty;
    private string _country = string.Empty;
    private string _description = string.Empty;
    private decimal _averageCost;
    private AttendanceCategory _attendance = AttendanceCategory.UpTo1000;
    private bool _isCharitable;
    private EventType? _selectedType;
    private string _tagSearchText = string.Empty;
    private Tag? _selectedAvailableTag;
    private string _iconPath = string.Empty;
    private DateTime? _currentStart = DateTime.Today;
    private DateTime? _currentEnd = DateTime.Today;
    private DateTime? _previousStart = DateTime.Today;
    private DateTime? _previousEnd = DateTime.Today;
    private string _validationMessage = string.Empty;

    public EventEditorViewModel(Action backToList, Action addType, Action addTag, Event? eventItem = null)
    {
        _backToList = backToList;
        _addType = addType;
        _addTag = addTag;
        _originalId = eventItem?.Id;

        AvailableTagsView = CollectionViewSource.GetDefaultView(App.DataStore.Tags);
        AvailableTagsView.Filter = FilterAvailableTag;

        if (eventItem is not null)
        {
            LoadEvent(eventItem);
        }

        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(_backToList);
        AddTypeCommand = new RelayCommand(_addType);
        AddTagCommand = new RelayCommand(_addTag);
        AddSelectedTagCommand = new RelayCommand(AddSelectedTag);
        RemoveSelectedTagCommand = new RelayCommand(RemoveSelectedTag);
        ChangeImageCommand = new RelayCommand(ChangeImage);
        RemoveImageCommand = new RelayCommand(() => IconPath = string.Empty);
        AddPreviousDateCommand = new RelayCommand(AddPreviousDate);
        RemovePreviousDateCommand = new RelayCommand(RemovePreviousDate);
    }

    public string PageTitle => _originalId is null ? GetString("NewEventLabel") : GetString("EditEventLabel");

    public IReadOnlyList<AttendanceCategory> AttendanceOptions { get; } = Enum.GetValues<AttendanceCategory>();

    public ObservableCollection<EventType> EventTypes => App.DataStore.EventTypes;

    public ObservableCollection<Tag> SelectedTags { get; } = [];

    public ObservableCollection<PreviousDateEditorItem> PreviousDates { get; } = [];

    public ICollectionView AvailableTagsView { get; }

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

    public string City
    {
        get => _city;
        set => SetProperty(ref _city, value);
    }

    public string Country
    {
        get => _country;
        set => SetProperty(ref _country, value);
    }

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public decimal AverageCost
    {
        get => _averageCost;
        set => SetProperty(ref _averageCost, value);
    }

    public AttendanceCategory Attendance
    {
        get => _attendance;
        set => SetProperty(ref _attendance, value);
    }

    public bool IsCharitable
    {
        get => _isCharitable;
        set => SetProperty(ref _isCharitable, value);
    }

    public EventType? SelectedType
    {
        get => _selectedType;
        set
        {
            if (SetProperty(ref _selectedType, value))
            {
                OnPropertyChanged(nameof(EffectiveIconPreviewPath));
            }
        }
    }

    public string TagSearchText
    {
        get => _tagSearchText;
        set
        {
            if (SetProperty(ref _tagSearchText, value))
            {
                AvailableTagsView.Refresh();
            }
        }
    }

    public Tag? SelectedAvailableTag
    {
        get => _selectedAvailableTag;
        set => SetProperty(ref _selectedAvailableTag, value);
    }

    public string IconPath
    {
        get => _iconPath;
        set
        {
            if (SetProperty(ref _iconPath, value))
            {
                OnPropertyChanged(nameof(EffectiveIconPreviewPath));
            }
        }
    }

    public string? EffectiveIconPreviewPath
    {
        get
        {
            string eventIcon = ImagePathService.ToImageSourcePath(IconPath);
            if (!string.IsNullOrWhiteSpace(eventIcon))
            {
                return eventIcon;
            }

            string typeIcon = ImagePathService.ToImageSourcePath(SelectedType?.IconKey);
            return string.IsNullOrWhiteSpace(typeIcon) ? null : typeIcon;
        }
    }

    public DateTime? CurrentStart
    {
        get => _currentStart;
        set => SetProperty(ref _currentStart, value);
    }

    public DateTime? CurrentEnd
    {
        get => _currentEnd;
        set => SetProperty(ref _currentEnd, value);
    }

    public DateTime? PreviousStart
    {
        get => _previousStart;
        set => SetProperty(ref _previousStart, value);
    }

    public DateTime? PreviousEnd
    {
        get => _previousEnd;
        set => SetProperty(ref _previousEnd, value);
    }

    public string ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    public ICommand SaveCommand { get; }

    public ICommand CancelCommand { get; }

    public ICommand AddTypeCommand { get; }

    public ICommand AddTagCommand { get; }

    public ICommand AddSelectedTagCommand { get; }

    public ICommand RemoveSelectedTagCommand { get; }

    public ICommand ChangeImageCommand { get; }

    public ICommand RemoveImageCommand { get; }

    public ICommand AddPreviousDateCommand { get; }

    public ICommand RemovePreviousDateCommand { get; }

    private void LoadEvent(Event eventItem)
    {
        Code = eventItem.Id;
        Name = eventItem.Name;
        City = eventItem.City;
        Country = eventItem.Country;
        Description = eventItem.Description;
        AverageCost = eventItem.AverageCost;
        Attendance = eventItem.Attendance;
        IsCharitable = eventItem.IsCharitable;
        SelectedType = App.DataStore.EventTypes.FirstOrDefault(type => type.Id == eventItem.TypeId);
        IconPath = eventItem.IconPath ?? string.Empty;
        CurrentStart = eventItem.CurrentStart == default ? eventItem.Date : eventItem.CurrentStart;
        CurrentEnd = eventItem.CurrentEnd == default ? eventItem.Date : eventItem.CurrentEnd;

        foreach (EventTag relation in App.DataStore.EventTags.Where(item => item.EventId == eventItem.Id))
        {
            Tag? tag = App.DataStore.Tags.FirstOrDefault(item => item.Id == relation.TagId);
            if (tag is not null)
            {
                SelectedTags.Add(tag);
            }
        }

        foreach (PreviousDate previousDate in App.DataStore.PreviousDates.Where(item => item.EventId == eventItem.Id))
        {
            PreviousDates.Add(new PreviousDateEditorItem
            {
                Start = previousDate.Start == default ? previousDate.Date : previousDate.Start,
                End = previousDate.End == default ? previousDate.Date : previousDate.End
            });
        }
    }

    private bool FilterAvailableTag(object item)
    {
        if (item is not Tag tag)
        {
            return false;
        }

        if (SelectedTags.Any(selected => selected.Id == tag.Id))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(TagSearchText))
        {
            return true;
        }

        string query = TagSearchText.Trim();
        return tag.Id.Contains(query, StringComparison.OrdinalIgnoreCase) ||
               tag.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
               tag.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
               tag.ColorHex.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private void AddSelectedTag()
    {
        if (SelectedAvailableTag is null || SelectedTags.Any(tag => tag.Id == SelectedAvailableTag.Id))
        {
            return;
        }

        SelectedTags.Add(SelectedAvailableTag);
        SelectedAvailableTag = null;
        AvailableTagsView.Refresh();
    }

    private void RemoveSelectedTag(object? parameter)
    {
        if (parameter is Tag tag)
        {
            SelectedTags.Remove(tag);
            AvailableTagsView.Refresh();
        }
    }

    private void AddPreviousDate()
    {
        if (PreviousStart is null || PreviousEnd is null)
        {
            ValidationMessage = "Previous date start and end are required.";
            return;
        }

        if (PreviousStart > PreviousEnd)
        {
            ValidationMessage = "Previous date start must be before or equal to end.";
            return;
        }

        PreviousDates.Add(new PreviousDateEditorItem { Start = PreviousStart.Value, End = PreviousEnd.Value });
        ValidationMessage = string.Empty;
    }

    private void RemovePreviousDate(object? parameter)
    {
        if (parameter is PreviousDateEditorItem previousDate)
        {
            PreviousDates.Remove(previousDate);
        }
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
            IconPath = MakeRelativeWhenPossible(dialog.FileName);
        }
    }

    private void Save()
    {
        if (!Validate())
        {
            return;
        }

        Event eventItem;
        DateTime now = DateTime.Today;

        if (_originalId is null)
        {
            eventItem = new Event
            {
                CreatedAt = now,
                X = 120,
                Y = 120,
                IsPlacedOnMap = false
            };
            App.DataStore.Events.Add(eventItem);
        }
        else
        {
            eventItem = App.DataStore.Events.First(item => item.Id == _originalId);
            if (!string.Equals(_originalId, Code, StringComparison.Ordinal))
            {
                foreach (EventTag relation in App.DataStore.EventTags.Where(item => item.EventId == _originalId))
                {
                    relation.EventId = Code;
                }

                foreach (PreviousDate previousDate in App.DataStore.PreviousDates.Where(item => item.EventId == _originalId))
                {
                    previousDate.EventId = Code;
                }
            }
        }

        eventItem.Id = Code;
        eventItem.Name = Name.Trim();
        eventItem.City = City.Trim();
        eventItem.Country = Country.Trim();
        eventItem.Description = Description.Trim();
        eventItem.AverageCost = AverageCost;
        eventItem.Attendance = Attendance;
        eventItem.IsCharitable = IsCharitable;
        eventItem.TypeId = SelectedType!.Id;
        eventItem.IconPath = string.IsNullOrWhiteSpace(IconPath) ? null : IconPath.Trim();
        eventItem.CurrentStart = CurrentStart!.Value;
        eventItem.CurrentEnd = CurrentEnd!.Value;
        eventItem.Date = CurrentStart.Value;
        eventItem.UpdatedAt = now;

        ReplaceRelations(eventItem.Id);
        App.DataService.SaveAll(App.DataStore);
        _backToList();
    }

    private void ReplaceRelations(string eventId)
    {
        foreach (EventTag relation in App.DataStore.EventTags.Where(item => item.EventId == eventId).ToList())
        {
            App.DataStore.EventTags.Remove(relation);
        }

        foreach (Tag tag in SelectedTags)
        {
            App.DataStore.EventTags.Add(new EventTag { EventId = eventId, TagId = tag.Id });
        }

        foreach (PreviousDate previousDate in App.DataStore.PreviousDates.Where(item => item.EventId == eventId).ToList())
        {
            App.DataStore.PreviousDates.Remove(previousDate);
        }

        int index = 1;
        foreach (PreviousDateEditorItem previousDate in PreviousDates)
        {
            App.DataStore.PreviousDates.Add(new PreviousDate
            {
                Id = $"{eventId}-PREV-{index}",
                EventId = eventId,
                Date = previousDate.Start,
                Start = previousDate.Start,
                End = previousDate.End
            });
            index++;
        }
    }

    private bool Validate()
    {
        if (string.IsNullOrWhiteSpace(Code))
        {
            ValidationMessage = "Code is required.";
            return false;
        }

        bool duplicate = App.DataStore.Events.Any(item =>
            !string.Equals(item.Id, _originalId, StringComparison.Ordinal) &&
            string.Equals(item.Id, Code, StringComparison.OrdinalIgnoreCase));

        if (duplicate)
        {
            ValidationMessage = "Code must be unique.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Name) ||
            string.IsNullOrWhiteSpace(City) ||
            string.IsNullOrWhiteSpace(Country) ||
            string.IsNullOrWhiteSpace(Description))
        {
            ValidationMessage = "Name, city, country, and description are required.";
            return false;
        }

        if (AverageCost < 0)
        {
            ValidationMessage = "Average cost cannot be negative.";
            return false;
        }

        if (SelectedType is null)
        {
            ValidationMessage = "Type is required.";
            return false;
        }

        if (CurrentStart is null || CurrentEnd is null)
        {
            ValidationMessage = "Current date start and end are required.";
            return false;
        }

        if (CurrentStart > CurrentEnd)
        {
            ValidationMessage = "Current date start must be before or equal to end.";
            return false;
        }

        ValidationMessage = string.Empty;
        return true;
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

    private static string GetString(string resourceKey)
    {
        return Application.Current.TryFindResource(resourceKey) as string ?? resourceKey;
    }

}
