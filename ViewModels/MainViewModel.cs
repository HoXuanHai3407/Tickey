using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using StickyNotePremium.Models;
using StickyNotePremium.Services;

namespace StickyNotePremium.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private static readonly CultureInfo VietnameseCulture = CultureInfo.GetCultureInfo("vi-VN");

    private readonly AppSettings _settings;
    private readonly CountdownCalculator _calculator;
    private readonly EventSelectionService _eventSelector;
    private readonly DispatcherTimer _timer;

    private HolidayEvent? _currentEvent;
    private HolidayEvent? _selectedManagedEvent;
    private bool _isAutoMode = true;
    private string _editEventName = string.Empty;
    private DateTime? _editTargetDate;
    private string _editTargetTimeText = "00:00";
    private string _backgroundColorText;
    private int _days;
    private int _hours;
    private int _minutes;
    private int _seconds;
    private string _summaryText = string.Empty;
    private string _monthSummaryText = string.Empty;
    private string _targetDateText = string.Empty;
    private string _validationMessage = string.Empty;
    private bool _isSettingsOpen;
    private Brush _solidBackgroundBrush = Brushes.Transparent;
    private Brush _backgroundBrush = Brushes.Transparent;
    private Brush _overlayBrush = Brushes.Transparent;
    private Brush _foregroundBrush = Brushes.White;

    public MainViewModel(
        AppSettings settings,
        CountdownCalculator calculator,
        EventSelectionService eventSelector)
    {
        _settings = settings;
        _calculator = calculator;
        _eventSelector = eventSelector;
        _backgroundColorText = settings.BackgroundColor;

        Events = new ObservableCollection<HolidayEvent>(
            _eventSelector.GetChronological(settings.Events ?? new List<HolidayEvent>())
                .Select(item => item.Clone()));

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += (_, _) => RefreshCountdown();

        RebuildVisuals();
        UseAutoMode();
        SelectedManagedEvent = CurrentEvent ?? Events.FirstOrDefault();
        if (SelectedManagedEvent is null)
        {
            BeginNewEvent();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler? SettingsChanged;

    public event Action<string>? BackgroundImageLoadFailed;

    public ObservableCollection<HolidayEvent> Events { get; }

    public HolidayEvent? CurrentEvent
    {
        get => _currentEvent;
        private set
        {
            if (ReferenceEquals(_currentEvent, value) ||
                (_currentEvent?.Id is not null && value?.Id == _currentEvent.Id))
            {
                _currentEvent = value;
                return;
            }

            _currentEvent = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CurrentEventName));
        }
    }

    public string CurrentEventName => CurrentEvent?.Name ?? "Không còn sự kiện sắp tới";

    public bool IsAutoMode
    {
        get => _isAutoMode;
        private set
        {
            if (SetDisplayField(ref _isAutoMode, value))
            {
                OnPropertyChanged(nameof(SelectionModeText));
            }
        }
    }

    public string SelectionModeText => IsAutoMode ? "AUTO • Lễ gần nhất" : "THỦ CÔNG • Bấm để AUTO";

    public HolidayEvent? SelectedManagedEvent
    {
        get => _selectedManagedEvent;
        set
        {
            if (ReferenceEquals(_selectedManagedEvent, value))
            {
                return;
            }

            _selectedManagedEvent = value;
            OnPropertyChanged();

            if (value is not null)
            {
                LoadEditor(value);
            }
        }
    }

    public string EditEventName
    {
        get => _editEventName;
        set => SetDisplayField(ref _editEventName, value ?? string.Empty);
    }

    public DateTime? EditTargetDate
    {
        get => _editTargetDate;
        set => SetDisplayField(ref _editTargetDate, value);
    }

    public string EditTargetTimeText
    {
        get => _editTargetTimeText;
        set => SetDisplayField(ref _editTargetTimeText, value ?? string.Empty);
    }

    public int Days
    {
        get => _days;
        private set => SetDisplayField(ref _days, value);
    }

    public int Hours
    {
        get => _hours;
        private set => SetDisplayField(ref _hours, value);
    }

    public int Minutes
    {
        get => _minutes;
        private set => SetDisplayField(ref _minutes, value);
    }

    public int Seconds
    {
        get => _seconds;
        private set => SetDisplayField(ref _seconds, value);
    }

    public string SummaryText
    {
        get => _summaryText;
        private set => SetDisplayField(ref _summaryText, value);
    }

    public string MonthSummaryText
    {
        get => _monthSummaryText;
        private set => SetDisplayField(ref _monthSummaryText, value);
    }

    public string TargetDateText
    {
        get => _targetDateText;
        private set => SetDisplayField(ref _targetDateText, value);
    }

    public Brush SolidBackgroundBrush
    {
        get => _solidBackgroundBrush;
        private set => SetDisplayField(ref _solidBackgroundBrush, value);
    }

    public Brush BackgroundBrush
    {
        get => _backgroundBrush;
        private set => SetDisplayField(ref _backgroundBrush, value);
    }

    public Brush OverlayBrush
    {
        get => _overlayBrush;
        private set => SetDisplayField(ref _overlayBrush, value);
    }

    public Brush ForegroundBrush
    {
        get => _foregroundBrush;
        private set => SetDisplayField(ref _foregroundBrush, value);
    }

    public bool IsSettingsOpen
    {
        get => _isSettingsOpen;
        set => SetDisplayField(ref _isSettingsOpen, value);
    }

    public bool AlwaysOnTop
    {
        get => _settings.AlwaysOnTop;
        set
        {
            if (_settings.AlwaysOnTop == value)
            {
                return;
            }

            _settings.AlwaysOnTop = value;
            OnPropertyChanged();
            RaiseSettingsChanged();
        }
    }

    public string BackgroundColor
    {
        get => _backgroundColorText;
        set
        {
            var newValue = value ?? string.Empty;
            if (_backgroundColorText == newValue)
            {
                return;
            }

            _backgroundColorText = newValue;
            OnPropertyChanged();

            if (!TryParseColor(newValue, out _))
            {
                ReportError("Màu nền phải ở dạng #RRGGBB hoặc #AARRGGBB.");
                return;
            }

            _settings.BackgroundColor = newValue.ToUpperInvariant();
            ClearValidationMessage();
            RebuildVisuals();
            RaiseSettingsChanged();
        }
    }

    public string? BackgroundImagePath => _settings.BackgroundImagePath;

    public double BackgroundImageOpacity
    {
        get => _settings.BackgroundImageOpacity;
        set
        {
            var clamped = Math.Clamp(value, 0.0, 1.0);
            if (Math.Abs(_settings.BackgroundImageOpacity - clamped) < 0.0001)
            {
                return;
            }

            _settings.BackgroundImageOpacity = clamped;
            OnPropertyChanged();
            RebuildVisuals();
            RaiseSettingsChanged();
        }
    }

    public double OverlayOpacity
    {
        get => _settings.OverlayOpacity;
        set
        {
            var clamped = Math.Clamp(value, 0.0, 1.0);
            if (Math.Abs(_settings.OverlayOpacity - clamped) < 0.0001)
            {
                return;
            }

            _settings.OverlayOpacity = clamped;
            OnPropertyChanged();
            RebuildVisuals();
            RaiseSettingsChanged();
        }
    }

    public string ImageStretchMode
    {
        get => _settings.ImageStretchMode;
        set
        {
            if (value is not ("UniformToFill" or "Uniform" or "Fill") || _settings.ImageStretchMode == value)
            {
                return;
            }

            _settings.ImageStretchMode = value;
            OnPropertyChanged();
            RebuildVisuals();
            RaiseSettingsChanged();
        }
    }

    public string ForegroundTheme
    {
        get => _settings.ForegroundTheme;
        set
        {
            if (value is not ("Auto" or "Light" or "Dark") || _settings.ForegroundTheme == value)
            {
                return;
            }

            _settings.ForegroundTheme = value;
            OnPropertyChanged();
            RebuildVisuals();
            RaiseSettingsChanged();
        }
    }

    public string ValidationMessage
    {
        get => _validationMessage;
        private set => SetDisplayField(ref _validationMessage, value);
    }

    public void Start()
    {
        RefreshCountdown();
        if (!_timer.IsEnabled)
        {
            _timer.Start();
        }
    }

    public void Stop() => _timer.Stop();

    public void UseAutoMode()
    {
        IsAutoMode = true;
        SetCurrentEvent(_eventSelector.GetNearestUpcoming(Events, DateTime.Now));
        RefreshCountdown();
    }

    public void ShowPreviousEvent()
    {
        if (Events.Count == 0)
        {
            SetCurrentEvent(null);
            RefreshCountdown();
            return;
        }

        IsAutoMode = false;
        var ordered = _eventSelector.GetChronological(Events);
        var currentIndex = FindEventIndex(ordered, CurrentEvent?.Id);
        var targetIndex = currentIndex < 0
            ? ordered.Count - 1
            : (currentIndex - 1 + ordered.Count) % ordered.Count;

        SetCurrentEvent(ordered[targetIndex]);
        SelectedManagedEvent = FindEventById(CurrentEvent?.Id);
        RefreshCountdown();
    }

    public void ShowNextEvent()
    {
        if (Events.Count == 0)
        {
            SetCurrentEvent(null);
            RefreshCountdown();
            return;
        }

        IsAutoMode = false;
        var ordered = _eventSelector.GetChronological(Events);
        var currentIndex = FindEventIndex(ordered, CurrentEvent?.Id);
        var targetIndex = currentIndex < 0 ? 0 : (currentIndex + 1) % ordered.Count;

        SetCurrentEvent(ordered[targetIndex]);
        SelectedManagedEvent = FindEventById(CurrentEvent?.Id);
        RefreshCountdown();
    }

    public void BeginNewEvent()
    {
        SelectedManagedEvent = null;
        EditEventName = string.Empty;
        EditTargetDate = DateTime.Today.AddDays(1);
        EditTargetTimeText = "00:00";
        ClearValidationMessage();
    }

    public bool SaveEditedEvent()
    {
        var name = EditEventName.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            ReportError("Hãy nhập tên sự kiện.");
            return false;
        }

        if (EditTargetDate is null)
        {
            ReportError("Hãy chọn ngày diễn ra hợp lệ.");
            return false;
        }

        if (!TryParseTime(EditTargetTimeText, out var time))
        {
            ReportError("Giờ phải có dạng HH:mm (ví dụ 18:30).");
            return false;
        }

        var target = DateTime.SpecifyKind(EditTargetDate.Value.Date + time, DateTimeKind.Local);
        var editingId = SelectedManagedEvent?.Id;
        HolidayEvent saved;

        if (editingId is null)
        {
            saved = new HolidayEvent
            {
                Name = name,
                TargetDateTime = target
            };
            Events.Add(saved);
        }
        else
        {
            var index = FindEventIndex(Events, editingId);
            if (index < 0)
            {
                ReportError("Không tìm thấy sự kiện cần sửa. Hãy chọn lại từ danh sách.");
                return false;
            }

            saved = new HolidayEvent
            {
                Id = editingId,
                Name = name,
                TargetDateTime = target
            };
            Events[index] = saved;
        }

        var currentId = CurrentEvent?.Id;
        SortEventsInPlace();
        SyncSettingsEvents();
        ClearValidationMessage();

        if (IsAutoMode)
        {
            SetCurrentEvent(_eventSelector.GetNearestUpcoming(Events, DateTime.Now));
        }
        else if (currentId == editingId || (currentId is null && editingId is null))
        {
            SetCurrentEvent(FindEventById(saved.Id));
        }
        else
        {
            SetCurrentEvent(FindEventById(currentId));
        }

        SelectedManagedEvent = FindEventById(saved.Id);
        RaiseSettingsChanged();
        RefreshCountdown();
        return true;
    }

    public bool DeleteSelectedEvent()
    {
        if (SelectedManagedEvent is null)
        {
            ReportError("Hãy chọn một sự kiện để xóa.");
            return false;
        }

        var deletingId = SelectedManagedEvent.Id;
        var currentId = CurrentEvent?.Id;
        var orderedBefore = _eventSelector.GetChronological(Events);
        var removedIndex = FindEventIndex(orderedBefore, deletingId);
        var existing = FindEventById(deletingId);
        if (existing is null)
        {
            ReportError("Không tìm thấy sự kiện cần xóa.");
            return false;
        }

        Events.Remove(existing);
        SortEventsInPlace();
        SyncSettingsEvents();

        if (IsAutoMode)
        {
            SetCurrentEvent(_eventSelector.GetNearestUpcoming(Events, DateTime.Now));
        }
        else if (currentId == deletingId)
        {
            var replacementIndex = Math.Clamp(removedIndex, 0, Math.Max(0, Events.Count - 1));
            SetCurrentEvent(Events.Count == 0 ? null : Events[replacementIndex]);
        }
        else
        {
            SetCurrentEvent(FindEventById(currentId));
        }

        SelectedManagedEvent = CurrentEvent ?? Events.FirstOrDefault();
        if (SelectedManagedEvent is null)
        {
            BeginNewEvent();
        }

        ClearValidationMessage();
        RaiseSettingsChanged();
        RefreshCountdown();
        return true;
    }

    public void RefreshCountdown()
    {
        var now = DateTime.Now;
        if (IsAutoMode)
        {
            var nearest = _eventSelector.GetNearestUpcoming(Events, now);
            if (nearest?.Id != CurrentEvent?.Id)
            {
                SetCurrentEvent(nearest);
            }
        }

        if (CurrentEvent is null)
        {
            Days = 0;
            Hours = 0;
            Minutes = 0;
            Seconds = 0;
            SummaryText = Events.Count == 0
                ? "Chưa có sự kiện nào — hãy thêm ngày lễ trong Cài đặt"
                : "Không còn sự kiện sắp tới";
            MonthSummaryText = Events.Count == 0
                ? "(Danh sách sự kiện đang trống)"
                : "(Các sự kiện đã qua vẫn được giữ để sửa hoặc xóa)";
            TargetDateText = "Không có mốc thời gian đang hoạt động";
            OnPropertyChanged(nameof(CurrentEventName));
            return;
        }

        var snapshot = _calculator.Calculate(now, CurrentEvent.TargetDateTime);
        Days = snapshot.Days;
        Hours = snapshot.Hours;
        Minutes = snapshot.Minutes;
        Seconds = snapshot.Seconds;

        SummaryText = snapshot.HasReached
            ? $"{CurrentEvent.Name} đã diễn ra"
            : $"Còn {snapshot.CalendarDays} ngày nữa đến {CurrentEvent.Name}";

        MonthSummaryText = snapshot.HasReached
            ? "(Sự kiện đã diễn ra — vẫn được lưu trong danh sách)"
            : $"(Quy đổi thành tháng: {snapshot.ApproxMonths} tháng {snapshot.ApproxDays} ngày)";

        TargetDateText = BuildTargetDateText(CurrentEvent.TargetDateTime);
    }

    public bool SetBackgroundImage(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        try
        {
            var brush = CreateImageBrush(path);
            _settings.BackgroundImagePath = path;
            BackgroundBrush = brush;
            OnPropertyChanged(nameof(BackgroundImagePath));
            ClearValidationMessage();
            RebuildForegroundAndOverlay();
            RaiseSettingsChanged();
            return true;
        }
        catch (Exception ex)
        {
            var message = $"Không thể đọc hình nền: {ex.Message}";
            ReportError(message);
            BackgroundImageLoadFailed?.Invoke(message);
            return false;
        }
    }

    public void ClearBackgroundImage()
    {
        if (string.IsNullOrWhiteSpace(_settings.BackgroundImagePath))
        {
            return;
        }

        _settings.BackgroundImagePath = null;
        OnPropertyChanged(nameof(BackgroundImagePath));
        ClearValidationMessage();
        RebuildVisuals();
        RaiseSettingsChanged();
    }

    public void ReportError(string message) => ValidationMessage = message;

    private void SetCurrentEvent(HolidayEvent? value)
    {
        var resolved = value is null ? null : FindEventById(value.Id) ?? value;
        CurrentEvent = resolved;
        OnPropertyChanged(nameof(CurrentEventName));
    }

    private void LoadEditor(HolidayEvent item)
    {
        EditEventName = item.Name;
        EditTargetDate = item.TargetDateTime.Date;
        EditTargetTimeText = item.TargetDateTime.ToString("HH:mm", CultureInfo.InvariantCulture);
        ClearValidationMessage();
    }

    private void SortEventsInPlace()
    {
        var ordered = _eventSelector.GetChronological(Events);
        Events.Clear();
        foreach (var item in ordered)
        {
            Events.Add(item);
        }
    }

    private void SyncSettingsEvents()
    {
        _settings.Events = _eventSelector.GetChronological(Events)
            .Select(item => item.Clone())
            .ToList();
    }

    private HolidayEvent? FindEventById(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        return Events.FirstOrDefault(item => string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    private static int FindEventIndex(IList<HolidayEvent> events, string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return -1;
        }

        for (var index = 0; index < events.Count; index++)
        {
            if (string.Equals(events[index].Id, id, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return -1;
    }

    private void RebuildVisuals()
    {
        if (!TryParseColor(_settings.BackgroundColor, out var color))
        {
            color = Color.FromRgb(201, 37, 51);
        }

        var solid = new SolidColorBrush(color);
        solid.Freeze();
        SolidBackgroundBrush = solid;

        var imagePath = _settings.BackgroundImagePath;
        if (!string.IsNullOrWhiteSpace(imagePath) && File.Exists(imagePath))
        {
            try
            {
                BackgroundBrush = CreateImageBrush(imagePath);
                RebuildForegroundAndOverlay();
                return;
            }
            catch (Exception ex)
            {
                var message = $"Hình nền không còn đọc được. Đang dùng màu nền dự phòng. {ex.Message}";
                ValidationMessage = message;
                BackgroundImageLoadFailed?.Invoke(message);
            }
        }
        else if (!string.IsNullOrWhiteSpace(imagePath))
        {
            ValidationMessage = "Không tìm thấy hình nền đã lưu. Đang dùng màu nền dự phòng.";
        }

        BackgroundBrush = Brushes.Transparent;
        RebuildForegroundAndOverlay();
    }

    private void RebuildForegroundAndOverlay()
    {
        var overlayAlpha = (byte)Math.Round(Math.Clamp(_settings.OverlayOpacity, 0.0, 1.0) * 255.0);
        var overlay = new SolidColorBrush(Color.FromArgb(overlayAlpha, 0, 0, 0));
        overlay.Freeze();
        OverlayBrush = overlay;

        Color foreground;
        switch (_settings.ForegroundTheme)
        {
            case "Dark":
                foreground = Color.FromRgb(31, 41, 55);
                break;
            case "Auto":
                foreground = ChooseAutomaticForeground();
                break;
            default:
                foreground = Colors.White;
                break;
        }

        var foregroundBrush = new SolidColorBrush(foreground);
        foregroundBrush.Freeze();
        ForegroundBrush = foregroundBrush;
    }

    private Color ChooseAutomaticForeground()
    {
        var imagePath = _settings.BackgroundImagePath;
        if (!string.IsNullOrWhiteSpace(imagePath) && File.Exists(imagePath))
        {
            return Colors.White;
        }

        if (!TryParseColor(_settings.BackgroundColor, out var color))
        {
            return Colors.White;
        }

        var luminance = (0.2126 * color.R) + (0.7152 * color.G) + (0.0722 * color.B);
        return luminance > 165 ? Color.FromRgb(31, 41, 55) : Colors.White;
    }

    private ImageBrush CreateImageBrush(string path)
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
        bitmap.UriSource = new Uri(Path.GetFullPath(path), UriKind.Absolute);
        bitmap.EndInit();
        bitmap.Freeze();

        var brush = new ImageBrush(bitmap)
        {
            Stretch = ParseStretch(_settings.ImageStretchMode),
            Opacity = Math.Clamp(_settings.BackgroundImageOpacity, 0.0, 1.0),
            AlignmentX = AlignmentX.Center,
            AlignmentY = AlignmentY.Center
        };
        brush.Freeze();
        return brush;
    }

    private static Stretch ParseStretch(string mode) => mode switch
    {
        "Uniform" => Stretch.Uniform,
        "Fill" => Stretch.Fill,
        _ => Stretch.UniformToFill
    };

    private static bool TryParseColor(string value, out Color color)
    {
        color = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var text = value.Trim();
        if (text.Length is not (7 or 9) || text[0] != '#')
        {
            return false;
        }

        try
        {
            var converted = ColorConverter.ConvertFromString(text);
            if (converted is Color parsed)
            {
                color = parsed;
                return true;
            }
        }
        catch
        {
            // Invalid user-entered color. Caller keeps the last valid color.
        }

        return false;
    }

    private static bool TryParseTime(string value, out TimeSpan time)
    {
        time = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (DateTime.TryParseExact(
                value.Trim(),
                new[] { "HH:mm", "H:mm" },
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsed))
        {
            time = parsed.TimeOfDay;
            return true;
        }

        return false;
    }

    private static string BuildTargetDateText(DateTime target)
    {
        var dayName = target.DayOfWeek switch
        {
            DayOfWeek.Monday => "Thứ 2",
            DayOfWeek.Tuesday => "Thứ 3",
            DayOfWeek.Wednesday => "Thứ 4",
            DayOfWeek.Thursday => "Thứ 5",
            DayOfWeek.Friday => "Thứ 6",
            DayOfWeek.Saturday => "Thứ 7",
            _ => "Chủ nhật"
        };

        var datePart = target.ToString("dd/MM/yyyy", VietnameseCulture);
        var timePart = target.TimeOfDay == TimeSpan.Zero ? string.Empty : $" {target:HH:mm}";
        return $"Ngày diễn ra (theo dương lịch): {dayName}, {datePart}{timePart}";
    }

    private void RaiseSettingsChanged() => SettingsChanged?.Invoke(this, EventArgs.Empty);

    private void ClearValidationMessage() => ValidationMessage = string.Empty;

    private bool SetDisplayField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
