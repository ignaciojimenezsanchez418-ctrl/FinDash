using Microsoft.Maui.Controls.Shapes;

namespace FinDashFrontEnd.Controls;

/// <summary>
/// Selector de opciones en una sola fila (24h · 7d · 30d · 1a, Compra | Venta, etc.).
/// Uso: &lt;c:SegmentedControl Items="{Binding Options}" SelectedIndex="{Binding Index}" /&gt;
/// </summary>
public class SegmentedControl : ContentView
{
    public static readonly BindableProperty ItemsProperty = BindableProperty.Create(
        nameof(Items), typeof(IList<string>), typeof(SegmentedControl),
        propertyChanged: (b, _, _) => ((SegmentedControl)b).Build());

    public static readonly BindableProperty SelectedIndexProperty = BindableProperty.Create(
        nameof(SelectedIndex), typeof(int), typeof(SegmentedControl), 0, BindingMode.TwoWay,
        propertyChanged: (b, _, _) => ((SegmentedControl)b).UpdateSelection());

    public IList<string>? Items
    {
        get => (IList<string>?)GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    readonly Grid _grid = new() { ColumnSpacing = 4 };
    readonly List<(Border Segment, Label Text)> _segments = [];

    public SegmentedControl()
    {
        var root = new Border
        {
            Padding = 4,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            Content = _grid,
        };
        root.SetDynamicResource(BackgroundColorProperty, "Surface2Color");
        Content = root;
    }

    void Build()
    {
        _grid.Children.Clear();
        _grid.ColumnDefinitions.Clear();
        _segments.Clear();
        if (Items is null) return;

        for (var i = 0; i < Items.Count; i++)
        {
            _grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            var label = new Label
            {
                Text = Items[i],
                FontSize = 14,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
            };
            var segment = new Border
            {
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 9 },
                HeightRequest = 36,
                Content = label,
            };
            SemanticProperties.SetDescription(segment, Items[i]);
            var index = i;
            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) => SelectedIndex = index;
            segment.GestureRecognizers.Add(tap);

            Grid.SetColumn(segment, i);
            _grid.Children.Add(segment);
            _segments.Add((segment, label));
        }
        UpdateSelection();
    }

    void UpdateSelection()
    {
        for (var i = 0; i < _segments.Count; i++)
        {
            var (segment, label) = _segments[i];
            var selected = i == SelectedIndex;
            segment.SetDynamicResource(BackgroundColorProperty, selected ? "SurfaceColor" : "Surface2Color");
            label.SetDynamicResource(Label.TextColorProperty, selected ? "TextColor" : "TextMutedColor");
            label.FontFamily = selected ? "InterSemiBold" : "InterMedium";
        }
    }
}
