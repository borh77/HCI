using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using StickItApp.ViewModels;

namespace StickItApp.Views;

public partial class MapPage : UserControl, IShortcutAwarePage
{
    private const string DragFormat = "StickItApp.MapEvent";

    private Point _dragStartPoint;
    private MapEventViewModel? _dragItem;
    private FrameworkElement? _dragSourceElement;

    public MapPage()
    {
        InitializeComponent();
    }

    public MapPage(MainWindowViewModel shell)
        : this()
    {
        DataContext = new MapViewModel(shell, shell.SetStatus);
    }

    private void EventCard_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        FrameworkElement? sourceElement = FindMapEventElement(sender);
        MapEventViewModel? item = FindMapEventViewModel(sender);
        if (sourceElement is null || item is null)
        {
            _dragItem = null;
            _dragSourceElement = null;
            return;
        }

        _dragStartPoint = e.GetPosition(this);
        _dragSourceElement = sourceElement;
        _dragItem = item;
    }

    private void EventCard_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        _dragItem ??= FindMapEventViewModel(sender);
        _dragSourceElement ??= FindMapEventElement(sender);
        if (_dragItem is null)
        {
            return;
        }

        Point currentPoint = e.GetPosition(this);
        if (Math.Abs(currentPoint.X - _dragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(currentPoint.Y - _dragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        SetDraggedItemVisual(true);

        try
        {
            Mouse.OverrideCursor = Cursors.SizeAll;
            DragDrop.DoDragDrop((DependencyObject)sender, new DataObject(DragFormat, _dragItem), DragDropEffects.Move);
        }
        finally
        {
            Mouse.OverrideCursor = null;
            SetDraggedItemVisual(false);
            _dragItem = null;
            _dragSourceElement = null;
        }
    }

    private void MapSurface_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DragFormat) ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private void MapSurface_Drop(object sender, DragEventArgs e)
    {
        if (DataContext is not MapViewModel viewModel ||
            e.Data.GetData(DragFormat) is not MapEventViewModel item)
        {
            return;
        }

        Point dropPoint = e.GetPosition(MapDropHost);
        double mapWidth = MapDropHost.Width;
        double mapHeight = MapDropHost.Height;

        viewModel.PlaceOnMap(
            item,
            dropPoint.X - MapViewModel.IconSize / 2,
            dropPoint.Y - MapViewModel.IconSize / 2,
            mapWidth,
            mapHeight);
        e.Handled = true;
    }

    private void UnplacedDropZone_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DragFormat) ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private void UnplacedDropZone_Drop(object sender, DragEventArgs e)
    {
        if (DataContext is MapViewModel viewModel &&
            e.Data.GetData(DragFormat) is MapEventViewModel item)
        {
            viewModel.ReturnToList(item);
            e.Handled = true;
        }
    }

    public bool FocusPrimarySearch()
    {
        MapFilterTextBox.Focus();
        MapFilterTextBox.SelectAll();
        return true;
    }

    public bool ResetFilters()
    {
        if (DataContext is MapViewModel viewModel)
        {
            SetDraggedItemVisual(false);
            _dragItem = null;
            _dragSourceElement = null;
            viewModel.ResetFilterCommand.Execute(null);
            return true;
        }

        return false;
    }

    public bool CancelOrBack()
    {
        if (DataContext is MapViewModel { SelectedEvent: not null } viewModel)
        {
            viewModel.SelectedEvent = null;
            return true;
        }

        return false;
    }

    private void SetDraggedItemVisual(bool isDragging)
    {
        if (_dragSourceElement is not null)
        {
            _dragSourceElement.Opacity = isDragging ? 0.55 : 1.0;
        }
    }

    private void EventTrayScrollLeft_Click(object sender, RoutedEventArgs e)
    {
        EventTrayScrollViewer.ScrollToHorizontalOffset(Math.Max(0, EventTrayScrollViewer.HorizontalOffset - 130));
    }

    private void EventTrayScrollRight_Click(object sender, RoutedEventArgs e)
    {
        EventTrayScrollViewer.ScrollToHorizontalOffset(EventTrayScrollViewer.HorizontalOffset + 130);
    }

    private void EventTrayScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        double delta = e.Delta < 0 ? 80 : -80;
        EventTrayScrollViewer.ScrollToHorizontalOffset(Math.Max(0, EventTrayScrollViewer.HorizontalOffset + delta));
        e.Handled = true;
    }

    private static MapEventViewModel? FindMapEventViewModel(object sender)
    {
        return FindMapEventElement(sender)?.DataContext as MapEventViewModel;
    }

    private static FrameworkElement? FindMapEventElement(object sender)
    {
        DependencyObject? current = sender as DependencyObject;
        while (current is not null)
        {
            if (current is FrameworkElement { DataContext: MapEventViewModel } element)
            {
                return element;
            }

            current = GetParent(current);
        }

        return null;
    }

    private static DependencyObject? GetParent(DependencyObject current)
    {
        return current is Visual or Visual3D
            ? VisualTreeHelper.GetParent(current)
            : LogicalTreeHelper.GetParent(current);
    }
}
