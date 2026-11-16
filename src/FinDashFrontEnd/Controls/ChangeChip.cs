using Microsoft.Maui.Controls.Shapes;

namespace FinDashFrontEnd.Controls;

/// <summary>
/// Variación en verde (ganancia) o rojo (pérdida). El texto ya trae ▲ o ▼, así que
/// no depende solo del color. Filled = false lo muestra sin fondo (filas de listas).
/// </summary>
public class ChangeChip : ContentView
{
    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(ChangeChip), string.Empty,
        propertyChanged: (b, _, n) => ((ChangeChip)b)._label.Text = (string)n);

    public static readonly BindableProperty IsPositiveProperty = BindableProperty.Create(
        nameof(IsPositive), typeof(bool), typeof(ChangeChip), true,
        propertyChanged: (b, _, _) => ((ChangeChip)b).ApplyColors());

    public static readonly BindableProperty FilledProperty = BindableProperty.Create(
        nameof(Filled), typeof(bool), typeof(ChangeChip), true,
        propertyChanged: (b, _, _) => ((ChangeChip)b).ApplyColors());

    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public bool IsPositive { get => (bool)GetValue(IsPositiveProperty); set => SetValue(IsPositiveProperty, value); }
    public bool Filled { get => (bool)GetValue(FilledProperty); set => SetValue(FilledProperty, value); }

    readonly Label _label = new() { FontFamily = "InterSemiBold", FontSize = 13 };
    readonly Border _border;

    public ChangeChip()
    {
        _border = new Border
        {
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 999 },
            Content = _label,
        };
        Content = _border;
        ApplyColors();
    }

    void ApplyColors()
    {
        _label.SetDynamicResource(Label.TextColorProperty, IsPositive ? "GainColor" : "LossColor");
        if (Filled)
        {
            _border.Padding = new Thickness(10, 5);
            _border.SetDynamicResource(BackgroundColorProperty, IsPositive ? "GainSoftColor" : "LossSoftColor");
        }
        else
        {
            _border.Padding = 0;
            _border.SetDynamicResource(BackgroundColorProperty, "TransparentColor");
        }
    }
}
