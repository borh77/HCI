using System.Windows;
using System.Windows.Threading;
using StickItApp.Services;

namespace StickItApp.Views;

public partial class ConfirmationDialog : Window
{
    private static readonly TimeSpan MessageDisplayDuration = TimeSpan.FromSeconds(3);

    private readonly bool _isMessageOnly;
    private DispatcherTimer? _autoCloseTimer;

    public ConfirmationDialog(string title, string message, string confirmText, string cancelText, DialogKind kind, bool isMessageOnly)
    {
        InitializeComponent();
        _isMessageOnly = isMessageOnly;
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

        if (_isMessageOnly)
        {
            Loaded += StartAutoCloseTimer;
            Closed += (_, _) => StopAutoCloseTimer();
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        CloseDialog(false);
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        CloseDialog(true);
    }

    private void CloseDialog(bool result)
    {
        StopAutoCloseTimer();
        if (_isMessageOnly)
        {
            Close();
            return;
        }

        DialogResult = result;
    }

    private void StartAutoCloseTimer(object sender, RoutedEventArgs e)
    {
        Loaded -= StartAutoCloseTimer;
        _autoCloseTimer = new DispatcherTimer
        {
            Interval = MessageDisplayDuration
        };
        _autoCloseTimer.Tick += (_, _) =>
        {
            StopAutoCloseTimer();
            Close();
        };
        _autoCloseTimer.Start();
    }

    private void StopAutoCloseTimer()
    {
        _autoCloseTimer?.Stop();
        _autoCloseTimer = null;
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
