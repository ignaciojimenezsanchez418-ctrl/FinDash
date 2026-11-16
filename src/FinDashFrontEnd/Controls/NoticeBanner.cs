using FinDashFrontEnd.Theme;
using Microsoft.Maui.Controls.Shapes;

namespace FinDashFrontEnd.Controls;

/// <summary>
/// Aviso amarillo de una línea: "Sin conexión · datos de hace 12 min", permisos negados, etc.
/// </summary>
public class NoticeBanner : ContentView
{
    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(NoticeBanner), string.Empty,
        propertyChanged: (b, _, n) => ((NoticeBanner)b)._text.Text = (string)n);

    public static readonly BindableProperty IconProperty = BindableProperty.Create(
        nameof(Icon), typeof(string), typeof(NoticeBanner), Icons.WifiOff,
        propertyChanged: (b, _, n) => ((NoticeBanner)b)._icon.Text = (string)n);

    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public string Icon { get => (string)GetValue(IconProperty); set => SetValue(IconProperty, value); }

    readonly Label _icon = new() { FontFamily = "Icons", FontSize = 18, Text = Icons.WifiOff, VerticalOptions = LayoutOptions.Center };
    readonly Label _text = new() { FontFamily = "InterMedium", FontSize = 13, VerticalOptions = LayoutOptions.Center };

    public NoticeBanner()
    {
        _icon.SetDynamicResource(Label.TextColorProperty, "WarnColor");
        _text.SetDynamicResource(Label.TextColorProperty, "WarnColor");

        var row = new Grid { ColumnSpacing = 10, ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star) } };
        row.Add(_icon, 0);
        row.Add(_text, 1);

        var border = new Border
        {
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            Padding = new Thickness(14, 10),
            Content = row,
        };
        border.SetDynamicResource(BackgroundColorProperty, "WarnSoftColor");
        Content = border;
    }
}
