using System.Windows;
using StickItApp.Views;

namespace StickItApp.Services;

public enum DialogKind
{
    Info,
    Warning,
    Error
}

public static class AppDialogService
{
    public static bool Confirm(string titleKey, string messageKey, string confirmKey, string cancelKey)
    {
        return ConfirmText(GetString(titleKey), GetString(messageKey), GetString(confirmKey), GetString(cancelKey));
    }

    public static bool ConfirmText(
        string title,
        string message,
        string confirmText,
        string cancelText,
        DialogKind kind = DialogKind.Warning)
    {
        ConfirmationDialog dialog = new(title, message, confirmText, cancelText, kind, false);
        dialog.Owner = GetOwner();
        return dialog.ShowDialog() == true;
    }

    public static void ShowInfo(string titleKey, string messageKey)
    {
        ShowMessageText(GetString(titleKey), GetString(messageKey), DialogKind.Info);
    }

    public static void ShowWarning(string titleKey, string messageKey)
    {
        ShowMessageText(GetString(titleKey), GetString(messageKey), DialogKind.Warning);
    }

    public static void ShowError(string titleKey, string messageKey)
    {
        ShowMessageText(GetString(titleKey), GetString(messageKey), DialogKind.Error);
    }

    public static void ShowMessageText(string title, string message, DialogKind kind = DialogKind.Info)
    {
        ConfirmationDialog dialog = new(title, message, GetString("OkLabel"), string.Empty, kind, true);
        dialog.Owner = GetOwner();
        dialog.ShowDialog();
    }

    private static Window? GetOwner()
    {
        return Application.Current.Windows
                   .OfType<Window>()
                   .FirstOrDefault(window => window.IsActive)
               ?? Application.Current.MainWindow;
    }

    private static string GetString(string resourceKey)
    {
        return Application.Current.TryFindResource(resourceKey) as string ?? resourceKey;
    }
}
