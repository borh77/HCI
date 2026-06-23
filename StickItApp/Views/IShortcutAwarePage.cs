namespace StickItApp.Views;

public interface IShortcutAwarePage
{
    bool FocusPrimarySearch();

    bool ResetFilters();

    bool CancelOrBack();
}
