using System.Windows;

namespace StickItApp.Views;

public partial class ConfirmationDialog : Window
{
    public ConfirmationDialog(string title, string message, string confirmText, string cancelText)
    {
        InitializeComponent();
        DataContext = new ConfirmationDialogViewModel(title, message, confirmText, cancelText);
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private sealed record ConfirmationDialogViewModel(
        string Title,
        string Message,
        string ConfirmText,
        string CancelText);
}
