using Microsoft.Win32;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace AUREL.Desktop;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _clock = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly DispatcherTimer _systemTimer = new() { Interval = TimeSpan.FromSeconds(2) };
    private readonly string _appDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AUREL");
    private string SettingsPath => Path.Combine(_appDirectory, "settings.json");
    private string CursorDirectory => Path.Combine(_appDirectory, "Cursors");
    private DesktopSettings _settings = new();
    private bool _allowClose;
    private ulong _lastIdle, _lastKernel, _lastUser;
    private bool _hasCpuSample;

    private static readonly ThemePalette Signature = new("#80231B18", "#BFAE987A", "#FF6F9B91", "#FFF3EFE5", "#FFD5C6B1");
    private static readonly ThemePalette Nocturne = new("#B5121518", "#BFAE987A", "#FF82A7A0", "#FFF2EEE5", "#FFD0C5B6");
    private static readonly ThemePalette Ivory = new("#DDF3EFE5", "#BFA38D6B", "#FF557C74", "#FF302923", "#FF74685B");

    public MainWindow()
    {
        InitializeComponent();
        _clock.Tick += (_, _) => UpdateClock();
        _systemTimer.Tick += (_, _) => UpdateSystemStatus();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        LoadSettings();
        ApplySettings();
        UpdateClock();
        UpdateSystemStatus();
        _clock.Start();
        if (_settings.ShowSystemStatus) _systemTimer.Start();
    }

    private void UpdateClock()
    {
        var now = DateTime.Now;
        TimeText.Text = now.ToString("hh:mm", CultureInfo.InvariantCulture);
        PeriodText.Text = now.ToString("tt", CultureInfo.InvariantCulture);
        DateText.Text = now.ToString("dddd  ·  dd MMMM yyyy", CultureInfo.InvariantCulture).ToUpperInvariant();
        WeekText.Text = $"WEEK {ISOWeek.GetWeekOfYear(now):00}  ·  DESKTOP";
    }

    private void UpdateSystemStatus()
    {
        if (!_settings.ShowSystemStatus) return;
        var memory = new MemoryStatusEx { Length = (uint)Marshal.SizeOf<MemoryStatusEx>() };
        var ram = GlobalMemoryStatusEx(ref memory) ? 100 - memory.MemoryLoad : 0;
        var cpu = "--";
        if (GetSystemTimes(out var idle, out var kernel, out var user))
        {
            var i = ToUInt64(idle); var k = ToUInt64(kernel); var u = ToUInt64(user);
            if (_hasCpuSample)
            {
                var total = (k - _lastKernel) + (u - _lastUser);
                var idleDelta = i - _lastIdle;
                if (total > 0) cpu = Math.Clamp((int)(100.0 * (total - idleDelta) / total), 0, 100).ToString(CultureInfo.InvariantCulture);
            }
            _lastIdle = i; _lastKernel = k; _lastUser = u; _hasCpuSample = true;
        }
        SystemText.Text = $"CPU  {cpu}%   RAM  {ram}%";
    }

    private static ulong ToUInt64(FileTime value) => ((ulong)value.High << 32) | value.Low;

    private void ApplySettings()
    {
        var palette = _settings.Theme switch { "Nocturne" => Nocturne, "Ivory" => Ivory, _ => Signature };
        PanelBorder.Background = BrushWithOpacity(palette.Panel, _settings.PanelOpacity);
        PanelBorder.BorderBrush = Brush(palette.Border);
        AccentBar.Background = Brush(palette.Accent);
        TimeText.Foreground = Brush(palette.Primary);
        PeriodText.Foreground = Brush(palette.Secondary);
        DateText.Foreground = Brush(palette.Secondary);
        var font = new FontFamily(_settings.FontFamily);
        TimeText.FontFamily = font; DateText.FontFamily = font; PeriodText.FontFamily = font;
        SystemText.FontFamily = font; WeekText.FontFamily = font;
        SystemText.Visibility = _settings.ShowSystemStatus ? Visibility.Visible : Visibility.Collapsed;
        SystemWidgetItem.IsChecked = _settings.ShowSystemStatus;
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

    private static SolidColorBrush Brush(string hex) => new((Color)ColorConverter.ConvertFromString(hex));
    private static SolidColorBrush BrushWithOpacity(string hex, double opacity)
    {
        var color = (Color)ColorConverter.ConvertFromString(hex);
        color.A = (byte)Math.Clamp(opacity * 255, 0, 255);
        return new SolidColorBrush(color);
    }

    private void SetTheme(string theme) { _settings.Theme = theme; ApplySettings(); SaveSettings(); }
    private void SignatureTheme_Click(object sender, RoutedEventArgs e) => SetTheme("Signature");
    private void NocturneTheme_Click(object sender, RoutedEventArgs e) => SetTheme("Nocturne");
    private void IvoryTheme_Click(object sender, RoutedEventArgs e) => SetTheme("Ivory");
    private void SetFont(string name) { _settings.FontFamily = name; ApplySettings(); SaveSettings(); }
    private void TimesFont_Click(object sender, RoutedEventArgs e) => SetFont("Times New Roman");
    private void GeorgiaFont_Click(object sender, RoutedEventArgs e) => SetFont("Georgia");
    private void GaramondFont_Click(object sender, RoutedEventArgs e) => SetFont("Garamond");
    private void PalatinoFont_Click(object sender, RoutedEventArgs e) => SetFont("Palatino Linotype");
    private void CambriaFont_Click(object sender, RoutedEventArgs e) => SetFont("Cambria");
    private void SetOpacity(double value) { _settings.PanelOpacity = value; ApplySettings(); SaveSettings(); }
    private void Opacity35_Click(object sender, RoutedEventArgs e) => SetOpacity(0.35);
    private void Opacity50_Click(object sender, RoutedEventArgs e) => SetOpacity(0.50);
    private void Opacity65_Click(object sender, RoutedEventArgs e) => SetOpacity(0.65);
    private void Opacity80_Click(object sender, RoutedEventArgs e) => SetOpacity(0.80);

    private void SystemWidget_Click(object sender, RoutedEventArgs e)
    {
        _settings.ShowSystemStatus = SystemWidgetItem.IsChecked;
        if (_settings.ShowSystemStatus) { UpdateSystemStatus(); _systemTimer.Start(); }
        else _systemTimer.Stop();
        ApplySettings(); SaveSettings();
    }

    private void LockPosition_Click(object sender, RoutedEventArgs e) { _settings.LockPosition = LockPositionItem.IsChecked; SaveSettings(); }
    private void AlwaysOnTop_Click(object sender, RoutedEventArgs e) { _settings.AlwaysOnTop = AlwaysOnTopItem.IsChecked; Topmost = _settings.AlwaysOnTop; SaveSettings(); }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState != MouseButtonState.Pressed || _settings.LockPosition) return;
        try { DragMove(); _settings.Left = Left; _settings.Top = Top; SaveSettings(); }
        catch (InvalidOperationException) { }
    }

    private void InstallCursor_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Directory.CreateDirectory(CursorDirectory);
            var arrow = CreateCursorFile("AUREL-Arrow.cur", CursorKind.Arrow);
            var busy = CreateCursorFile("AUREL-Busy.cur", CursorKind.Busy);
            var wait = CreateCursorFile("AUREL-Wait.cur", CursorKind.Wait);
            var help = CreateCursorFile("AUREL-Help.cur", CursorKind.Help);
            var ibeam = CreateCursorFile("AUREL-IBeam.cur", CursorKind.IBeam);
            var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Cursors", writable: true)
                ?? throw new InvalidOperationException("Windows cursor settings could not be opened.");
            using (key)
            {
                if (!File.Exists(Path.Combine(_appDirectory, "cursor-backup.json")))
                {
                    var backup = new Dictionary<string, string?>();
                    foreach (var name in new[] { "Arrow", "AppStarting", "Wait", "Help", "IBeam" })
                        backup[name] = key.GetValue(name) as string;
                    File.WriteAllText(Path.Combine(_appDirectory, "cursor-backup.json"), JsonSerializer.Serialize(backup));
                }
                key.SetValue("Arrow", arrow); key.SetValue("AppStarting", busy);
                key.SetValue("Wait", wait); key.SetValue("Help", help); key.SetValue("IBeam", ibeam);
            }
            SystemParametersInfo(0x0057, 0, IntPtr.Zero, 0);
            MessageBox.Show("AUREL cursor scheme applied. Use Restore previous cursor scheme to revert.", "AUREL", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) { MessageBox.Show($"Cursor installation failed: {ex.Message}", "AUREL", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }

    private void RestoreCursor_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var backupPath = Path.Combine(_appDirectory, "cursor-backup.json");
            if (!File.Exists(backupPath)) { MessageBox.Show("No saved cursor backup was found.", "AUREL"); return; }
            var backup = JsonSerializer.Deserialize<Dictionary<string, string?>>(File.ReadAllText(backupPath)) ?? new();
            using var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Cursors", writable: true)
                ?? throw new InvalidOperationException("Windows cursor settings could not be opened.");
            foreach (var pair in backup)
                if (pair.Value is null) key.DeleteValue(pair.Key, throwOnMissingValue: false);
                else key.SetValue(pair.Key, pair.Value);
            SystemParametersInfo(0x0057, 0, IntPtr.Zero, 0);
            MessageBox.Show("Your previous Windows cursor settings have been restored.", "AUREL", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) { MessageBox.Show($"Cursor restore failed: {ex.Message}", "AUREL", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }

    private enum CursorKind { Arrow, Busy, Wait, Help, IBeam }
    private string CreateCursorFile(string fileName, CursorKind kind)
    {
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            var ivory = new SolidColorBrush(Color.FromRgb(245, 240, 229));
            var taupe = new Pen(new SolidColorBrush(Color.FromRgb(112, 96, 80)), 1.8);
            var teal = new SolidColorBrush(Color.FromRgb(111, 155, 145));
            if (kind == CursorKind.IBeam)
            {
                dc.DrawLine(taupe, new Point(16, 5), new Point(16, 27));
                dc.DrawLine(new Pen(ivory, 2.2), new Point(16, 5), new Point(16, 27));
                dc.DrawLine(taupe, new Point(9, 5), new Point(23, 5)); dc.DrawLine(taupe, new Point(9, 27), new Point(23, 27));
            }
            else
            {
                var geometry = Geometry.Parse("M 3,2 L 3,26 L 9,20 L 14,30 L 19,27 L 14,17 L 23,17 Z");
                dc.DrawGeometry(ivory, taupe, geometry);
                if (kind == CursorKind.Busy || kind == CursorKind.Wait)
                {
                    dc.DrawEllipse(teal, null, new Point(23, 24), 5, 5);
                    if (kind == CursorKind.Wait) dc.DrawEllipse(null, new Pen(ivory, 1.2), new Point(23, 24), 2.5, 2.5);
                }
                if (kind == CursorKind.Help) dc.DrawEllipse(teal, null, new Point(23, 24), 2, 2);
            }
        }
        var bitmap = new RenderTargetBitmap(32, 32, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var png = new MemoryStream(); encoder.Save(png); var image = png.ToArray();
        using var output = new BinaryWriter(File.Create(Path.Combine(CursorDirectory, fileName)));
        output.Write((ushort)0); output.Write((ushort)2); output.Write((ushort)1);
        output.Write((byte)32); output.Write((byte)32); output.Write((byte)0); output.Write((byte)0);
        output.Write((ushort)2); output.Write((ushort)2); output.Write((uint)image.Length); output.Write((uint)22);
        output.Write(image);
        return Path.Combine(CursorDirectory, fileName);
    }

    private void Exit_Click(object sender, RoutedEventArgs e) { _allowClose = true; Close(); }
    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (!_allowClose) { _settings.Left = Left; _settings.Top = Top; SaveSettings(); }
        _clock.Stop(); _systemTimer.Stop();
    }

    private void LoadSettings()
    {
        try
        {
            if (File.Exists(SettingsPath))
                _settings = JsonSerializer.Deserialize<DesktopSettings>(File.ReadAllText(SettingsPath)) ?? new DesktopSettings();
        }
        catch { _settings = new DesktopSettings(); }
    }

    private void SaveSettings()
    {
        try { Directory.CreateDirectory(_appDirectory); File.WriteAllText(SettingsPath, JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true })); }
        catch { }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct FileTime { public uint Low; public uint High; }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MemoryStatusEx
    {
        public uint Length; public uint MemoryLoad;
        public ulong TotalPhysical; public ulong AvailablePhysical;
        public ulong TotalPageFile; public ulong AvailablePageFile;
        public ulong TotalVirtual; public ulong AvailableVirtual;
        public ulong AvailableExtendedVirtual;
    }
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetSystemTimes(out FileTime idle, out FileTime kernel, out FileTime user);
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SystemParametersInfo(uint action, uint param, IntPtr value, uint winIni);

    private sealed class DesktopSettings
    {
        public string Theme { get; set; } = "Signature";
        public string FontFamily { get; set; } = "Times New Roman";
        public double PanelOpacity { get; set; } = 0.50;
        public bool ShowSystemStatus { get; set; }
        public bool LockPosition { get; set; }
        public bool AlwaysOnTop { get; set; } = true;
        public double? Left { get; set; }
        public double? Top { get; set; }
    }
    private sealed record ThemePalette(string Panel, string Border, string Accent, string Primary, string Secondary);
}