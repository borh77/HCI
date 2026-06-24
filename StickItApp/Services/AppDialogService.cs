using System.Windows;
using StickItApp.Views;

namespace StickItApp.Services;

public static class AppDialogService
{
    public static bool Confirm(string titleKey, string messageKey, string confirmKey, string cancelKey)
    {
        ConfirmationDialog dialog = new(
            GetString(titleKey),
            GetString(messageKey),
            GetString(confirmKey),
            GetString(cancelKey));

        dialog.Owner = GetOwner();
        return dialog.ShowDialog() == true;
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
