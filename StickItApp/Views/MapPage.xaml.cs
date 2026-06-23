using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using StickItApp.ViewModels;

namespace StickItApp.Views;

public partial class MapPage : UserControl, IShortcutAwarePage
{
    private const string DragFormat = "StickItApp.MapEvent";

    private Point _dragStartPoint;
    private MapEventViewModel? _dragItem;

    public MapPage()
    {
        InitializeComponent();
    }

    public MapPage(MainWindowViewModel shell)
        : this()
    {
        DataContext = new MapViewModel(shell);
    }

    private void EventCard_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(this);
        _dragItem = (sender as FrameworkElement)?.DataContext as MapEventViewModel;
    }

    private void EventCard_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _dragItem is null)
        {
            return;
        }

        Point currentPoint = e.GetPosition(this);
        if (Math.Abs(currentPoint.X - _dragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(currentPoint.Y - _dragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        DragDrop.DoDragDrop((DependencyObject)sender, new DataObject(DragFormat, _dragItem), DragDropEffects.Move);
        _dragItem = null;
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
        viewModel.PlaceOnMap(
            item,
            dropPoint.X - MapViewModel.IconSize / 2,
            dropPoint.Y - MapViewModel.IconSize / 2,
            MapDropHost.ActualWidth,
            MapDropHost.ActualHeight);
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
}
