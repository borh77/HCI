using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using StickItApp.Commands;
using StickItApp.Views;

namespace StickItApp.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
{
    private bool _isMenuOpen = true;
    private UserControl _currentPage;
    private string _currentPageTitle;
    private string _selectedLanguage = "EN";
    private bool _isDarkTheme;

    public MainWindowViewModel()
    {
        _currentPage = new MapPage();
        _currentPageTitle = "Map";

        ToggleMenuCommand = new RelayCommand(() => IsMenuOpen = !IsMenuOpen);
        ToggleThemeCommand = new RelayCommand(ToggleTheme);
        ShowMapCommand = new RelayCommand(() => Navigate(new MapPage(), "Map"));
        ShowEventsCommand = new RelayCommand(() => Navigate(new EventListPage(), "Events"));
        ShowTypesCommand = new RelayCommand(() => Navigate(new TypeListPage(), "Types"));
        ShowTagsCommand = new RelayCommand(() => Navigate(new TagListPage(), "Tags"));
        ShowSettingsCommand = new RelayCommand(() => Navigate(new SettingsPage(), "Settings"));
        NewEventCommand = new RelayCommand(() => Navigate(new EventEditorPage(), "New Event"));
        NewTypeCommand = new RelayCommand(() => Navigate(new TypeEditorPage(), "New Type"));
        NewTagCommand = new RelayCommand(() => Navigate(new TagEditorPage(), "New Tag"));
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

    public string CurrentPageTitle
    {
        get => _currentPageTitle;
        private set => SetProperty(ref _currentPageTitle, value);
    }

    public string SelectedLanguage
    {
        get => _selectedLanguage;
        set => SetProperty(ref _selectedLanguage, value);
    }

    public string ThemeButtonText => _isDarkTheme ? "Dark" : "Light";

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

    private void Navigate(UserControl page, string title)
    {
        CurrentPage = page;
        CurrentPageTitle = title;
    }

    private void ToggleTheme()
    {
        _isDarkTheme = !_isDarkTheme;
        OnPropertyChanged(nameof(ThemeButtonText));
    }
}
