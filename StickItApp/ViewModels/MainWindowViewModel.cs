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
    private sealed record NavigationEntry(UserControl Page, string TitleKey, bool IsMainPage);

    private readonly Stack<NavigationEntry> _backStack = [];
    private bool _isMenuOpen;
    private UserControl _currentPage;
    private string _currentPageTitleKey;
    private bool _isOnMainPage = true;
    private string _selectedLanguage;
    private bool _isDarkTheme;
    private string _statusMessage = string.Empty;
    private bool _isStatusError;

    public MainWindowViewModel()
    {
        _currentPage = new MapPage(this);
        _currentPageTitleKey = "MapLabel";
        _selectedLanguage = PersonalizationService.NormalizeLanguage(App.DataStore.Settings.Language);
        _isDarkTheme = PersonalizationService.NormalizeTheme(App.DataStore.Settings.Theme) == "Dark";

        ToggleMenuCommand = new RelayCommand(() => IsMenuOpen = !IsMenuOpen);
        BackCommand = new RelayCommand(GoBack);
        ToggleThemeCommand = new RelayCommand(ToggleTheme);
        ShowMapCommand = new RelayCommand(NavigateToMap);
        ShowEventsCommand = new RelayCommand(NavigateToEventList);
        ShowTypesCommand = new RelayCommand(NavigateToTypeList);
        ShowTagsCommand = new RelayCommand(NavigateToTagList);
        ShowSearchCommand = new RelayCommand(NavigateToSearch);
        NewEventCommand = new RelayCommand(() => NavigateToEventEditor(null));
        NewTypeCommand = new RelayCommand(() => NavigateToTypeEditor(null));
        NewTagCommand = new RelayCommand(() => NavigateToTagEditor(null));
    }

    public bool IsMenuOpen
    {
        get => _isMenuOpen;
        set => SetProperty(ref _isMenuOpen, value);
    }

    public UserControl CurrentPage
    {
        get => _currentPage;
        private set => SetProperty(ref _currentPage, value);
    }

    public string CurrentPageTitle => GetString(_currentPageTitleKey);

    public bool CanGoBack => _backStack.Count > 0 || !IsOnMainPage;

    public bool IsOnMainPage
    {
        get => _isOnMainPage;
        private set
        {
            if (SetProperty(ref _isOnMainPage, value))
            {
                OnPropertyChanged(nameof(ShowMenuButton));
                OnPropertyChanged(nameof(ShowBackButton));
                OnPropertyChanged(nameof(TopBarTitle));
            }
        }
    }

    public bool ShowMenuButton => IsOnMainPage;

    public bool ShowBackButton => !IsOnMainPage;

    public string TopBarTitle => IsOnMainPage ? GetString("ApplicationName") : CurrentPageTitle;

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
                OnPropertyChanged(nameof(TopBarTitle));
                OnPropertyChanged(nameof(ThemeButtonText));
            }
        }
    }

    public string ThemeButtonText => _isDarkTheme ? GetString("DarkThemeLabel") : GetString("LightThemeLabel");

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool IsStatusError
    {
        get => _isStatusError;
        private set => SetProperty(ref _isStatusError, value);
    }

    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

    public ICommand ToggleMenuCommand { get; }
    public ICommand BackCommand { get; }
    public ICommand ToggleThemeCommand { get; }
    public ICommand ShowMapCommand { get; }
    public ICommand ShowEventsCommand { get; }
    public ICommand ShowTypesCommand { get; }
    public ICommand ShowTagsCommand { get; }
    public ICommand ShowSearchCommand { get; }
    public ICommand NewEventCommand { get; }
    public ICommand NewTypeCommand { get; }
    public ICommand NewTagCommand { get; }

    private void Navigate(Func<UserControl> createPage, string titleKey, bool isMainPage = false, bool addToHistory = true)
    {
        if (addToHistory)
        {
            _backStack.Push(new NavigationEntry(CurrentPage, _currentPageTitleKey, IsOnMainPage));
        }

        CurrentPage = createPage();
        _currentPageTitleKey = titleKey;
        IsOnMainPage = isMainPage;
        IsMenuOpen = false;
        OnPropertyChanged(nameof(CurrentPageTitle));
        OnPropertyChanged(nameof(TopBarTitle));
        OnPropertyChanged(nameof(CanGoBack));
    }

    private void NavigateToMap()
    {
        _backStack.Clear();
        Navigate(() => new MapPage(this), "MapLabel", isMainPage: true, addToHistory: false);
    }

    public void GoBack()
    {
        IsMenuOpen = false;

        if (_backStack.Count == 0)
        {
            NavigateToMap();
            return;
        }

        NavigationEntry previous = _backStack.Pop();
        CurrentPage = previous.Page;
        _currentPageTitleKey = previous.TitleKey;
        IsOnMainPage = previous.IsMainPage;
        OnPropertyChanged(nameof(CurrentPageTitle));
        OnPropertyChanged(nameof(TopBarTitle));
        OnPropertyChanged(nameof(CanGoBack));
    }

    public void NavigateToTypeList()
    {
        Navigate(() => new TypeListPage(this), "TypesLabel");
    }

    public void NavigateToEventList()
    {
        Navigate(() => new EventListPage(this), "EventsLabel");
    }

    public void NavigateToSearch()
    {
        Navigate(() => new SearchPage(this), "SearchLabel");
    }

    public void NavigateToEventEditor(Event? eventItem)
    {
        Navigate(() => new EventEditorPage(this, eventItem), eventItem is null ? "NewEventLabel" : "EventEditorLabel");
    }

    public void NavigateToEventDetails(Event eventItem)
    {
        Navigate(() => new EventDetailsPage(this, eventItem), "EventDetailsLabel");
    }

    public void NavigateToTypeEditor(EventType? type)
    {
        Navigate(() => new TypeEditorPage(this, type), type is null ? "NewTypeLabel" : "TypeEditorLabel");
    }

    public void NavigateToTagList()
    {
        Navigate(() => new TagListPage(this), "TagsLabel");
    }

    public void NavigateToTagEditor(Tag? tag)
    {
        Navigate(() => new TagEditorPage(this, tag), tag is null ? "NewTagLabel" : "TagEditorLabel");
    }

    public void SetStatus(string message, bool isError = false)
    {
        StatusMessage = message;
        IsStatusError = isError;
        OnPropertyChanged(nameof(HasStatusMessage));
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
