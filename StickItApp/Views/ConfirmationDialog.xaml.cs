using System.Windows;
using StickItApp.Services;

namespace StickItApp.Views;

public partial class ConfirmationDialog : Window
{
    public ConfirmationDialog(string title, string message, string confirmText, string cancelText, DialogKind kind, bool isMessageOnly)
    {
        InitializeComponent();
        DataContext = new ConfirmationDialogViewModel(
            title,
            message,
            confirmText,
            cancelText,
            isMessageOnly,
            GetIconText(kind),
            GetIconForeground(kind),
            GetIconBackground(kind),
            kind == DialogKind.Error);
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private static string GetIconText(DialogKind kind)
    {
        return kind switch
        {
            DialogKind.Info => "\uE946",
            DialogKind.Error => "\uEA39",
            _ => "\uE7BA"
        };
    }

    private static string GetIconForeground(DialogKind kind)
    {
        return kind switch
        {
            DialogKind.Info => "#2563EB",
            DialogKind.Error => "#DC2626",
            _ => "#D97706"
        };
    }

    private static string GetIconBackground(DialogKind kind)
    {
        return kind switch
        {
            DialogKind.Info => "#DBEAFE",
            DialogKind.Error => "#FEE2E2",
            _ => "#FEF3C7"
        };
    }

    private sealed record ConfirmationDialogViewModel(
        string Title,
        string Message,
        string ConfirmText,
        string CancelText,
        bool IsMessageOnly,
        string IconText,
        string IconForeground,
        string IconBackground,
        bool IsDanger);
}
