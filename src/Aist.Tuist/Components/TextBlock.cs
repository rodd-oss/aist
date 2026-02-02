using System.Linq;
using Aist.Tuist.Primitives;
using Aist.Tuist.Rendering;

namespace Aist.Tuist.Components;

public class TextBlock : TuiElement
{
    private string _text = string.Empty;
    public string Text 
    { 
        get => _text; 
        set => _text = value ?? string.Empty; 
    }

    public TuiColor Foreground { get; set; } = TuiColor.Default;
    public TuiColor Background { get; set; } = TuiColor.Default;
    public bool TextWrapping { get; set; }

    private List<string> _wrappedLines = new();

    protected override Size MeasureOverride(Size availableSize)
    {
        if (string.IsNullOrEmpty(Text)) return Size.Zero;

        var normalizedText = Text.Replace("\r\n", "\n", StringComparison.Ordinal);
        if (!TextWrapping)
        {
            var lines = normalizedText.Split('\n');
            var width = lines.Length > 0 ? lines.Max(l => l.Length) : 0;
            var height = lines.Length;
            return new Size(width, height);
        }
        else
        {
            _wrappedLines = WrapText(normalizedText, availableSize.Width);
            var width = _wrappedLines.Count > 0 ? _wrappedLines.Max(l => l.Length) : 0;
            var height = _wrappedLines.Count;
            return new Size(width, height);
        }
    }

    protected override void ArrangeOverride(Rect finalRect)
    {
        if (TextWrapping)
        {
            var normalizedText = Text.Replace("\r\n", "\n", StringComparison.Ordinal);
            _wrappedLines = WrapText(normalizedText, finalRect.Width);
        }
    }

    public override void OnRender(DrawingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var style = new TuiStyle(Foreground, Background);
        var normalizedText = Text.Replace("\r\n", "\n", StringComparison.Ordinal);
        var lines = TextWrapping ? (IEnumerable<string>)_wrappedLines : normalizedText.Split('\n');
        var padding = Padding;

        int i = 0;
        var availableWidth = Math.Max(0, ActualBounds.Width - padding.Horizontal);
        var availableHeight = Math.Max(0, ActualBounds.Height - padding.Vertical);

        foreach (var line in lines)
        {
            if (i >= availableHeight) break;
            
            var renderLine = line;
            if (renderLine.Length > availableWidth)
            {
                renderLine = renderLine.Substring(0, availableWidth);
            }
            
            context.DrawString(padding.Left, padding.Top + i, renderLine, style);
            i++;
        }
    }

    private static List<string> WrapText(string text, int maxWidth)
    {
        if (maxWidth <= 0) return new List<string> { text };
        
        var result = new List<string>();
        var lines = text.Split('\n');

        foreach (var line in lines)
        {
            if (line.Length <= maxWidth)
            {
                result.Add(line);
                continue;
            }

            var words = line.Split(' ');
            var currentLine = string.Empty;

            foreach (var word in words)
            {
                if (currentLine.Length + (currentLine.Length > 0 ? 1 : 0) + word.Length <= maxWidth)
                {
                    if (currentLine.Length > 0) currentLine += " ";
                    currentLine += word;
                }
                else
                {
                    if (!string.IsNullOrEmpty(currentLine))
                    {
                        result.Add(currentLine);
                    }
                    
                    var remainingWord = word;
                    while (remainingWord.Length > maxWidth)
                    {
                        result.Add(remainingWord.Substring(0, maxWidth));
                        remainingWord = remainingWord.Substring(maxWidth);
                    }
                    currentLine = remainingWord;
                }
            }

            if (!string.IsNullOrEmpty(currentLine))
            {
                result.Add(currentLine);
            }
        }

        return result;
    }
}
