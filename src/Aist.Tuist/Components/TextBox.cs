using Aist.Tuist.Events;
using Aist.Tuist.Primitives;
using Aist.Tuist.Rendering;

namespace Aist.Tuist.Components;

public class TextBox : TuiElement
{
    private string _text = string.Empty;
    public string Text 
    { 
        get => _text; 
        set 
        {
            _text = value ?? string.Empty;
            _caretIndex = Math.Clamp(_caretIndex, 0, _text.Length);
        }
    }

    private int _caretIndex;
    public int CaretIndex
    {
        get => _caretIndex;
        set => _caretIndex = Math.Clamp(value, 0, Text.Length);
    }

    public TextBox()
    {
        IsFocusable = true;
        KeyDown += OnKeyDown;
    }

    private void OnKeyDown(object? sender, RoutedEventArgs e)
    {
        if (e is not KeyRoutedEventArgs ke) return;

        if (ke.Key == ConsoleKey.Backspace)
        {
            if (CaretIndex > 0)
            {
                Text = Text.Remove(CaretIndex - 1, 1);
                CaretIndex--;
                e.Handled = true;
            }
        }
        else if (ke.Key == ConsoleKey.Delete)
        {
            if (CaretIndex < Text.Length)
            {
                Text = Text.Remove(CaretIndex, 1);
                e.Handled = true;
            }
        }
        else if (ke.Key == ConsoleKey.LeftArrow)
        {
            CaretIndex--;
            e.Handled = true;
        }
        else if (ke.Key == ConsoleKey.RightArrow)
        {
            CaretIndex++;
            e.Handled = true;
        }
        else if (ke.Key == ConsoleKey.Home)
        {
            CaretIndex = 0;
            e.Handled = true;
        }
        else if (ke.Key == ConsoleKey.End)
        {
            CaretIndex = Text.Length;
            e.Handled = true;
        }
        else if (!char.IsControl(ke.KeyChar))
        {
            Text = Text.Insert(CaretIndex, ke.KeyChar.ToString());
            CaretIndex++;
            e.Handled = true;
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        // Return 1 line height, and either the full text length OR the constrained width
        var desiredWidth = Math.Max(10, Text.Length + 1);
        return new Size(Math.Min(desiredWidth, availableSize.Width), 1);
    }

    public override void OnRender(DrawingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var background = IsFocused ? TuiColor.Blue : TuiColor.BrightBlack;
        var foreground = TuiColor.White;
        var width = ActualBounds.Width;

        context.FillRectangle(new Rect(0, 0, width, ActualBounds.Height), ' ', new TuiStyle(foreground, background));
        
        // Calculate scroll offset to keep caret visible
        int offset = 0;
        if (width > 0)
        {
            if (CaretIndex >= width)
            {
                offset = CaretIndex - width + 1;
            }
        }
        
        var textToDraw = "";
        if (Text.Length > offset)
        {
            textToDraw = Text.Substring(offset);
            if (textToDraw.Length > width) textToDraw = textToDraw.Substring(0, width);
        }

        context.DrawString(0, 0, textToDraw, new TuiStyle(foreground, background));

        if (IsFocused)
        {
            // Simple cursor representation
            int renderX = CaretIndex - offset;
            if (renderX >= 0 && renderX < width)
            {
                char cursorChar = CaretIndex < Text.Length ? Text[CaretIndex] : ' ';
                context.DrawChar(renderX, 0, cursorChar, new TuiStyle(TuiColor.Black, TuiColor.White));
            }
        }
    }
}
