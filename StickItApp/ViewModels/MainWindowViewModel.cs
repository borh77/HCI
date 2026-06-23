using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using StickItApp.Commands;
using StickItApp.Models;
using StickItApp.Services;
using StickItApp.Views;

namespace StickItApp.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
{
    private bool _isMenuOpen = true;
    private UserControl _currentPage;
    private string _currentPageTitleKey;
    private string _selectedLanguage;
    private bool _isDarkTheme;

    public MainWindowViewModel()
    {
        _currentPage = new MapPage();
        _currentPageTitleKey = "MapLabel";
        _selectedLanguage = PersonalizationService.NormalizeLanguage(App.DataStore.Settings.Language);
        _isDarkTheme = PersonalizationService.NormalizeTheme(App.DataStore.Settings.Theme) == "Dark";

        ToggleMenuCommand = new RelayCommand(() => IsMenuOpen = !IsMenuOpen);
        ToggleThemeCommand = new RelayCommand(ToggleTheme);
        ShowMapCommand = new RelayCommand(() => Navigate(new MapPage(), "MapLabel"));
        ShowEventsCommand = new RelayCommand(NavigateToEventList);
        ShowTypesCommand = new RelayCommand(NavigateToTypeList);
        ShowTagsCommand = new RelayCommand(NavigateToTagList);
        ShowSettingsCommand = new RelayCommand(() => Navigate(new SettingsPage(), "SettingsLabel"));
        NewEventCommand = new RelayCommand(() => NavigateToEventEditor(null));
        NewTypeCommand = new RelayCommand(() => NavigateToTypeEditor(null));
        NewTagCommand = new RelayCommand(() => NavigateToTagEditor(null));
    }

    public bool IsMenuOpen
    {
        get => _isMenuOpen;
        set
        {
            if (SetProperty(ref _isMenuOpen, value))
            {
                OnPropertyChanged(nameof(MenuColumnWidth));
            }
        }
    }

    public GridLength MenuColumnWidth => IsMenuOpen ? new GridLength(136) : new GridLength(0);

    public UserControl CurrentPage
    {
        get => _currentPage;
        private set => SetProperty(ref _currentPage, value);
    }

    public string CurrentPageTitle => GetString(_currentPageTitleKey);

    public string SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            string normalizedLanguage = PersonalizationService.NormalizeLanguage(value);
            if (SetProperty(ref _selectedLanguage, normalizedLanguage))
            {
                App.DataStore.Settings.Language = normalizedLanguage;
                App.DataService.SaveSettings(App.DataStore.Settings);
                PersonalizationService.ApplyLanguage(normalizedLanguage);
                OnPropertyChanged(nameof(CurrentPageTitle));
                OnPropertyChanged(nameof(ThemeButtonText));
            }
        }
    }

    public string ThemeButtonText => _isDarkTheme ? GetString("DarkThemeLabel") : GetString("LightThemeLabel");

    public ICommand ToggleMenuCommand { get; }
    public ICommand ToggleThemeCommand { get; }
    public ICommand ShowMapCommand { get; }
    public ICommand ShowEventsCommand { get; }
    public ICommand ShowTypesCommand { get; }
    public ICommand ShowTagsCommand { get; }
    public ICommand ShowSettingsCommand { get; }
    public ICommand NewEventCommand { get; }
    public ICommand NewTypeCommand { get; }
    public ICommand NewTagCommand { get; }

    private void Navigate(UserControl page, string titleKey)
    {
        CurrentPage = page;
        _currentPageTitleKey = titleKey;
        OnPropertyChanged(nameof(CurrentPageTitle));
    }

    public void NavigateToTypeList()
    {
        Navigate(new TypeListPage(this), "TypesLabel");
    }

    public void NavigateToEventList()
    {
        Navigate(new EventListPage(this), "EventsLabel");
    }

    public void NavigateToEventEditor(Event? eventItem)
    {
        Navigate(new EventEditorPage(this, eventItem), eventItem is null ? "NewEventLabel" : "EventEditorLabel");
    }

    public void NavigateToEventDetails(Event eventItem)
    {
        Navigate(new EventDetailsPage(this, eventItem), "EventDetailsLabel");
    }

    public void NavigateToTypeEditor(EventType? type)
    {
        Navigate(new TypeEditorPage(this, type), type is null ? "NewTypeLabel" : "TypeEditorLabel");
    }

    public void NavigateToTagList()
    {
        Navigate(new TagListPage(this), "TagsLabel");
    }

    public void NavigateToTagEditor(Tag? tag)
    {
        Navigate(new TagEditorPage(this, tag), tag is null ? "NewTagLabel" : "TagEditorLabel");
    }

    private void ToggleTheme()
    {
        _isDarkTheme = !_isDarkTheme;
        string theme = _isDarkTheme ? "Dark" : "Light";
        App.DataStore.Settings.Theme = theme;
        App.DataService.SaveSettings(App.DataStore.Settings);
        PersonalizationService.ApplyTheme(theme);
        OnPropertyChanged(nameof(ThemeButtonText));
    }

    private static string GetString(string resourceKey)
    {
        return Application.Current.TryFindResource(resourceKey) as string ?? resourceKey;
    }
}
