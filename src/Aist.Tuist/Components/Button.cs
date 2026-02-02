using Aist.Tuist.Events;
using Aist.Tuist.Primitives;
using Aist.Tuist.Rendering;

namespace Aist.Tuist.Components;

public class Button : TuiElement
{
    public static readonly RoutedEvent ClickEvent = new("Click", RoutingStrategy.Bubble, typeof(EventHandler<RoutedEventArgs>));

    public event EventHandler<RoutedEventArgs> Click { add => AddHandler(ClickEvent, value); remove => RemoveHandler(ClickEvent, value); }

    public string Text
    {
        get => _textBlock.Text;
        set => _textBlock.Text = value;
    }

    private readonly TextBlock _textBlock;

    public Button()
    {
        IsFocusable = true;
        _textBlock = new TextBlock
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        Children.Add(_textBlock);
        
        MouseDown += OnMouseDown;
        KeyDown += OnKeyDown;
    }

    private void OnMouseDown(object? sender, RoutedEventArgs e)
    {
        DispatchEvent(new RoutedEventArgs(ClickEvent));
        e.Handled = true;
    }

    private void OnKeyDown(object? sender, RoutedEventArgs e)
    {
        if (e is KeyRoutedEventArgs ke && (ke.Key == ConsoleKey.Enter || ke.Key == ConsoleKey.Spacebar))
        {
            DispatchEvent(new RoutedEventArgs(ClickEvent));
            e.Handled = true;
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        _textBlock.Measure(availableSize);
        return _textBlock.DesiredSize;
    }

    protected override void ArrangeOverride(Rect finalRect)
    {
        _textBlock.Arrange(finalRect);
    }

    public override void OnRender(DrawingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var background = IsFocused ? TuiColor.Blue : TuiColor.BrightBlack;
        var foreground = TuiColor.White;

        if (IsMouseOver)
        {
            background = IsFocused ? TuiColor.BrightBlue : TuiColor.Black;
        }

        context.FillRectangle(new Rect(0, 0, ActualBounds.Width, ActualBounds.Height), ' ', new TuiStyle(foreground, background));
        
        _textBlock.Foreground = foreground;
        _textBlock.Background = background;
        
        base.OnRender(context);
    }
}
