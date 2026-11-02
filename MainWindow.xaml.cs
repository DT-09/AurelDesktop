using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace AUREL.Desktop;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _clock = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly string _settingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AUREL", "settings.json");
    private DesktopSettings _settings = new();
    private bool _allowClose;

    private static readonly ThemePalette Signature = new(
        "#80231B18", "#BFAE987A", "#FF6F9B91",
        "#FFF3EFE5", "#FFD5C6B1");

    private static readonly ThemePalette Nocturne = new(
        "#B5121518", "#BFAE987A", "#FF82A7A0",
        "#FFF2EEE5", "#FFD0C5B6");

    private static readonly ThemePalette Ivory = new(
        "#DDF3EFE5", "#BFA38D6B", "#FF557C74",
        "#FF302923", "#FF74685B");

    public MainWindow()
    {
        InitializeComponent();
        _clock.Tick += (_, _) => UpdateClock();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        LoadSettings();
        ApplySettings();
        UpdateClock();
        _clock.Start();
    }

    private void UpdateClock()
    {
        var now = DateTime.Now;
        TimeText.Text = now.ToString("hh:mm", CultureInfo.InvariantCulture);
        PeriodText.Text = now.ToString("tt", CultureInfo.InvariantCulture);
        DateText.Text = now.ToString("dddd  ·  dd MMMM yyyy", CultureInfo.InvariantCulture).ToUpperInvariant();
        WeekText.Text = $"WEEK {ISOWeek.GetWeekOfYear(now):00}  ·  DESKTOP";
    }

    private void ApplySettings()
    {
        var palette = _settings.Theme switch
        {
            "Nocturne" => Nocturne,
            "Ivory" => Ivory,
            _ => Signature
        };

        PanelBorder.Background = Brush(palette.Panel);
        PanelBorder.BorderBrush = Brush(palette.Border);
        AccentBar.Background = Brush(palette.Accent);
        TimeText.Foreground = Brush(palette.Primary);
        PeriodText.Foreground = Brush(palette.Secondary);
        DateText.Foreground = Brush(palette.Secondary);

        SignatureThemeItem.IsChecked = _settings.Theme == "Signature";
        NocturneThemeItem.IsChecked = _settings.Theme == "Nocturne";
        IvoryThemeItem.IsChecked = _settings.Theme == "Ivory";
        LockPositionItem.IsChecked = _settings.LockPosition;
        AlwaysOnTopItem.IsChecked = _settings.AlwaysOnTop;
        Topmost = _settings.AlwaysOnTop;

        if (_settings.Left.HasValue && _settings.Top.HasValue)
        {
            var area = SystemParameters.WorkArea;
            Left = Math.Clamp(_settings.Left.Value, area.Left, Math.Max(area.Left, area.Right - Width));
            Top = Math.Clamp(_settings.Top.Value, area.Top, Math.Max(area.Top, area.Bottom - Height));
        }
        else
        {
            Left = SystemParameters.WorkArea.Left + (SystemParameters.WorkArea.Width - Width) / 2;
            Top = SystemParameters.WorkArea.Top + 54;
        }
    }

    private static SolidColorBrush Brush(string hex) =>
        new((Color)ColorConverter.ConvertFromString(hex));

    private void SetTheme(string theme)
    {
        _settings.Theme = theme;
        ApplySettings();
        SaveSettings();
    }

    private void SignatureTheme_Click(object sender, RoutedEventArgs e) => SetTheme("Signature");
    private void NocturneTheme_Click(object sender, RoutedEventArgs e) => SetTheme("Nocturne");
    private void IvoryTheme_Click(object sender, RoutedEventArgs e) => SetTheme("Ivory");

    private void LockPosition_Click(object sender, RoutedEventArgs e)
    {
        _settings.LockPosition = LockPositionItem.IsChecked;
        SaveSettings();
    }

    private void AlwaysOnTop_Click(object sender, RoutedEventArgs e)
    {
        _settings.AlwaysOnTop = AlwaysOnTopItem.IsChecked;
        Topmost = _settings.AlwaysOnTop;
        SaveSettings();
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState != MouseButtonState.Pressed || _settings.LockPosition) return;
        try
        {
            DragMove();
            _settings.Left = Left;
            _settings.Top = Top;
            SaveSettings();
        }
        catch (InvalidOperationException)
        {
            // Windows can cancel a drag when the pointer leaves the window.
        }
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        _allowClose = true;
        Close();
    }

    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (!_allowClose)
        {
            _settings.Left = Left;
            _settings.Top = Top;
            SaveSettings();
        }
        _clock.Stop();
    }

    private void LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var loaded = JsonSerializer.Deserialize<DesktopSettings>(File.ReadAllText(_settingsPath));
                if (loaded is not null) _settings = loaded;
            }
        }
        catch
        {
            _settings = new DesktopSettings();
        }
    }

    private void SaveSettings()
    {
        try
        {
            var directory = Path.GetDirectoryName(_settingsPath);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(_settingsPath, JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            // AUREL remains usable if settings cannot be written.
        }
    }

    private sealed class DesktopSettings
    {
        public string Theme { get; set; } = "Signature";
        public bool LockPosition { get; set; }
        public bool AlwaysOnTop { get; set; } = true;
        public double? Left { get; set; }
        public double? Top { get; set; }
    }

    private sealed record ThemePalette(
        string Panel, string Border, string Accent,
        string Primary, string Secondary);
}
