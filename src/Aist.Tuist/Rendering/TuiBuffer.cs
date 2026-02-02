using System.Globalization;
using System.Text;
using Aist.Tuist.Primitives;

namespace Aist.Tuist.Rendering;

public sealed class TuiBuffer
{
    private struct Cell
    {
        public char Char;
        public TuiStyle Style;
    }

    private Cell[] _buffer;
    private Cell[] _previousBuffer;
    private int _width;
    private int _height;

    public int Width => _width;
    public int Height => _height;

    public TuiBuffer(int width, int height)
    {
        _width = width;
        _height = height;
        _buffer = new Cell[width * height];
        _previousBuffer = new Cell[width * height];
        Clear();
    }

    public void Resize(int width, int height)
    {
        if (_width == width && _height == height) return;

        _width = width;
        _height = height;
        _buffer = new Cell[width * height];
        _previousBuffer = new Cell[width * height];
        Clear();
    }

    public void SetCell(int x, int y, char c, TuiStyle style)
    {
        if (x < 0 || x >= _width || y < 0 || y >= _height) return;
        int index = y * _width + x;
        _buffer[index] = new Cell { Char = c, Style = style };
    }

    public void Clear()
    {
        for (int i = 0; i < _buffer.Length; i++)
        {
            _buffer[i] = new Cell { Char = ' ', Style = TuiStyle.Default };
        }
    }

    public void Flush()
    {
        var sb = new StringBuilder();
        TuiStyle currentStyle = new TuiStyle((TuiColor)(-2), (TuiColor)(-2)); // Force initial style set

        for (int y = 0; y < _height; y++)
        {
            bool cursorMoved = false;
            for (int x = 0; x < _width; x++)
            {
                int index = y * _width + x;
                var current = _buffer[index];
                var previous = _previousBuffer[index];

                if (current.Char != previous.Char || current.Style != previous.Style)
                {
                    if (!cursorMoved)
                    {
                        sb.Append(CultureInfo.InvariantCulture, $"\u001b[{y + 1};{x + 1}H");
                        cursorMoved = true;
                    }
                    else
                    {
                        // If we are already at the right position, we don't need to move the cursor
                        // But if we skipped some cells, we might need to move it.
                        // For simplicity, let's just move it if it's not the next cell.
                        // Actually, if we are iterating sequentially, we only need to move it once per changed segment.
                    }

                    if (current.Style != currentStyle)
                    {
                        ApplyStyle(sb, current.Style);
                        currentStyle = current.Style;
                    }

                    sb.Append(current.Char);
                    _previousBuffer[index] = current;
                }
                else
                {
                    cursorMoved = false; // Next changed cell will need a cursor move
                }
            }
        }

        if (sb.Length > 0)
        {
            Console.Write(sb.ToString());
            Console.Out.Flush();
        }
    }

    private static void ApplyStyle(StringBuilder sb, TuiStyle style)
    {
        sb.Append("\u001b[0m"); // Reset
        if (style.Foreground != TuiColor.Default)
        {
            int code = (int)style.Foreground;
            if (code < 8) sb.Append(CultureInfo.InvariantCulture, $"\u001b[{30 + code}m");
            else sb.Append(CultureInfo.InvariantCulture, $"\u001b[{90 + (code - 8)}m");
        }
        if (style.Background != TuiColor.Default)
        {
            int code = (int)style.Background;
            if (code < 8) sb.Append(CultureInfo.InvariantCulture, $"\u001b[{40 + code}m");
            else sb.Append(CultureInfo.InvariantCulture, $"\u001b[{100 + (code - 8)}m");
        }
    }
}
