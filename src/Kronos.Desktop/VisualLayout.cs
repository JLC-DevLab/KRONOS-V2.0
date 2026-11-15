// Copyright 2026 JLC / UlysesX12. Volatility Software License 1.0.
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Kronos.Desktop;

public sealed partial class MainWindow
{
    private static string FormatBytes(long bytes) => bytes >= 1024L * 1024 * 1024 ? $"{bytes / (1024.0 * 1024 * 1024):N2} GiB" :
        bytes >= 1024 * 1024 ? $"{bytes / (1024.0 * 1024):N1} MiB" : bytes >= 1024 ? $"{bytes / 1024.0:N1} KiB" : $"{bytes} bytes";
    private static Border Card(Control content, double padding = 20) => new()
    {
        Background = Brushes.White, BorderBrush = Brush.Parse("#DEE6EF"), BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(10), Padding = new Thickness(padding), Child = content
    };
    private static StackPanel PageHeading(string title, string description) => new()
    {
        Spacing = 6, Children = {
            new TextBlock { Text = title, FontSize = 23, FontWeight = FontWeight.SemiBold, Foreground = Brush.Parse("#18364F") },
            new TextBlock { Text = description, FontSize = 14, Foreground = Brush.Parse("#536579"), TextWrapping = TextWrapping.Wrap }
        }
    };
    private static Button Primary(Button button)
    {
        button.Background = Brush.Parse("#166EA5"); button.Foreground = Brushes.White;
        return button;
    }
    private static TabItem Page(string label, Control content) => new()
    {
        Header = new TextBlock { Text = label, Width = 160, FontSize = 14, Margin = new Thickness(8, 10), FontWeight = FontWeight.Medium },
        Content = new Border { Padding = new Thickness(20, 6, 0, 0), Child = content },
        HorizontalContentAlignment = HorizontalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Stretch
    };
}
