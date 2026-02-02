using Spectre.Console;
using Spectre.Console.Rendering;

namespace Aist.Cli.UI;

internal record struct TuiCell(char Char, Style Style);

internal sealed class TuiBuffer
{
    private TuiCell[] _frontBuffer = Array.Empty<TuiCell>();
    private TuiCell[] _backBuffer = Array.Empty<TuiCell>();
    private int _width;
    private int _height;

    public int Width => _width;
    public int Height => _height;

    public void Resize(int width, int height)
    {
        if (width == _width && height == _height) return;

        _width = width;
        _height = height;
        int size = width * height;
        _frontBuffer = new TuiCell[size];
        _backBuffer = new TuiCell[size];
        
        // Initialize buffers
        for (int i = 0; i < size; i++)
        {
            _frontBuffer[i] = new TuiCell(' ', Style.Plain);
            _backBuffer[i] = new TuiCell('\0', Style.Plain);
        }
        
        AnsiConsole.Clear();
    }

    public void Clear()
    {
        int size = _width * _height;
        for (int i = 0; i < size; i++)
        {
            _frontBuffer[i] = new TuiCell(' ', Style.Plain);
        }
    }

    public void Draw(IRenderable renderable, int x, int y, int? maxWidth = null, int? maxHeight = null)
    {
        var options = new RenderOptions(AnsiConsole.Console.Profile.Capabilities, new Size(_width, _height));
        var segments = renderable.Render(options, maxWidth ?? (_width - x));
        
        int currentX = x;
        int currentY = y;

        foreach (var segment in segments)
        {
            if (segment.IsControlCode) continue;

            var parts = segment.Text.Replace("\r", "").Split('\n');
            for (int i = 0; i < parts.Length; i++)
            {
                if (i > 0)
                {
                    currentY++;
                    currentX = x;
                }

                if (currentY >= _height) break;

                foreach (var c in parts[i])
                {
                    if (currentX < _width && currentX >= 0)
                    {
                        _frontBuffer[currentX + currentY * _width] = new TuiCell(c, segment.Style ?? Style.Plain);
                    }
                    currentX++;
                }
            }
        }
    }

    public void Flush()
    {
        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                int idx = x + y * _width;
                var front = _frontBuffer[idx];
                var back = _backBuffer[idx];

                if (front != back)
                {
                    int startX = x;
                    var batchStyle = front.Style;
                    var batchText = new System.Text.StringBuilder();

                    while (x < _width)
                    {
                        int currentIdx = x + y * _width;
                        var cell = _frontBuffer[currentIdx];
                        if (cell.Style != batchStyle || cell == _backBuffer[currentIdx])
                            break;

                        batchText.Append(cell.Char);
                        _backBuffer[currentIdx] = cell;
                        x++;
                    }

                    Console.SetCursorPosition(startX, y);
                    AnsiConsole.Write(new Text(batchText.ToString(), batchStyle));
                    x--; 
                }
            }
        }
    }
}
