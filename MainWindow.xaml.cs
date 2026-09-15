using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using Microsoft.Win32;
using StickyNotePremium.Models;
using StickyNotePremium.Services;
using StickyNotePremium.ViewModels;

namespace StickyNotePremium;

public partial class MainWindow : Window
{
    private readonly SettingsService _settingsService;
    private readonly AppSettings _settings;
    private readonly MainViewModel _viewModel;
    private readonly DispatcherTimer _saveTimer;
    private bool _restoringWindow;

    public MainWindow()
    {
        InitializeComponent();

        _settingsService = new SettingsService();
        _settings = _settingsService.Load();
        _viewModel = new MainViewModel(_settings, new CountdownCalculator(), new EventSelectionService());
        DataContext = _viewModel;

        _viewModel.SettingsChanged += ViewModel_SettingsChanged;

        _saveTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(450)
        };
        _saveTimer.Tick += SaveTimer_Tick;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _restoringWindow = true;
        try
        {
            Width = Math.Max(MinWidth, _settings.WindowWidth);
            Height = Math.Max(MinHeight, _settings.WindowHeight);
            Left = _settings.WindowLeft;
            Top = _settings.WindowTop;
            EnsureWindowVisible();
        }
        finally
        {
            _restoringWindow = false;
        }

        _viewModel.Start();
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        _saveTimer.Stop();
        _viewModel.Stop();
        PersistSettings();
    }

    private void ViewModel_SettingsChanged(object? sender, EventArgs e)
    {
        ScheduleSave();
    }

    private void SaveTimer_Tick(object? sender, EventArgs e)
    {
        _saveTimer.Stop();
        PersistSettings();
    }

    private void Window_LocationChanged(object? sender, EventArgs e)
    {
        if (!_restoringWindow && WindowState == WindowState.Normal)
        {
            ScheduleSave();
        }
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (!_restoringWindow && WindowState == WindowState.Normal)
        {
            ScheduleSave();
        }
    }

    private void MainSurface_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || IsInteractiveElement(e.OriginalSource as DependencyObject))
        {
            return;
        }

        try
        {
            DragMove();
        }
        catch (InvalidOperationException)
        {
            // DragMove can throw if the button state changes between the event and the call.
        }
    }

    private static bool IsInteractiveElement(DependencyObject? source)
    {
        while (source is not null)
        {
            if (source is ButtonBase or TextBoxBase or Selector or DatePicker or Slider or Thumb)
            {
                return true;
            }

            source = source switch
            {
                Visual or Visual3D => VisualTreeHelper.GetParent(source),
                _ => LogicalTreeHelper.GetParent(source)
            };
        }

        return false;
    }

    private void PreviousEventButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ShowPreviousEvent();
    }

    private void NextEventButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ShowNextEvent();
    }

    private void AutoModeButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.UseAutoMode();
    }

    private void NewEventButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.BeginNewEvent();
    }

    private void SaveEventButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.SaveEditedEvent();
    }

    private void DeleteEventButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.DeleteSelectedEvent();
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.IsSettingsOpen = !_viewModel.IsSettingsOpen;
    }

    private void CloseSettings_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.IsSettingsOpen = false;
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void BrowseBackground_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Chọn hình nền cho Sticky Note Premium",
            Filter = "Hình ảnh|*.jpg;*.jpeg;*.png;*.bmp;*.webp|JPEG|*.jpg;*.jpeg|PNG|*.png|Bitmap|*.bmp|WebP (nếu Windows hỗ trợ)|*.webp|Tất cả tệp|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) == true)
        {
            _viewModel.SetBackgroundImage(dialog.FileName);
        }
    }

    private void ClearBackground_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ClearBackgroundImage();
    }

    private void ColorPreset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string color })
        {
            _viewModel.BackgroundColor = color;
        }
    }

    private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        var nextWidth = Math.Max(MinWidth, Width + e.HorizontalChange);
        var nextHeight = Math.Max(MinHeight, Height + e.VerticalChange);

        Width = nextWidth;
        Height = nextHeight;
        ScheduleSave();
    }

    private void ScheduleSave()
    {
        if (_restoringWindow)
        {
            return;
        }

        _saveTimer.Stop();
        _saveTimer.Start();
    }

    private void PersistSettings()
    {
        try
        {
            var bounds = WindowState == WindowState.Normal
                ? new Rect(Left, Top, ActualWidth > 0 ? ActualWidth : Width, ActualHeight > 0 ? ActualHeight : Height)
                : RestoreBounds;

            if (!bounds.IsEmpty && double.IsFinite(bounds.Left) && double.IsFinite(bounds.Top))
            {
                _settings.WindowLeft = bounds.Left;
                _settings.WindowTop = bounds.Top;
                _settings.WindowWidth = Math.Max(MinWidth, bounds.Width);
                _settings.WindowHeight = Math.Max(MinHeight, bounds.Height);
            }

            _settingsService.Save(_settings);
        }
        catch (Exception ex)
        {
            _viewModel.ReportError($"Không thể lưu cấu hình: {ex.Message}");
        }
    }

    private void EnsureWindowVisible()
    {
        var virtualLeft = SystemParameters.VirtualScreenLeft;
        var virtualTop = SystemParameters.VirtualScreenTop;
        var virtualWidth = SystemParameters.VirtualScreenWidth;
        var virtualHeight = SystemParameters.VirtualScreenHeight;
        var virtualRight = virtualLeft + virtualWidth;
        var virtualBottom = virtualTop + virtualHeight;

        var hasUsefulIntersection =
            Left + 80 >= virtualLeft &&
            Left <= virtualRight - 80 &&
            Top + 40 >= virtualTop &&
            Top <= virtualBottom - 40;

        if (!hasUsefulIntersection)
        {
            var workArea = SystemParameters.WorkArea;
            Left = workArea.Left + Math.Max(0, (workArea.Width - Width) / 2.0);
            Top = workArea.Top + Math.Max(0, (workArea.Height - Height) / 2.0);
            return;
        }

        Left = Math.Min(Math.Max(Left, virtualLeft - Width + 80), virtualRight - 80);
        Top = Math.Min(Math.Max(Top, virtualTop), virtualBottom - 40);
    }
}
