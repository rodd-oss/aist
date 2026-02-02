using Aist.Tuist.Primitives;

namespace Aist.Tuist.Rendering;

public sealed class DrawingContext
{
    private readonly TuiBuffer _buffer;
    private readonly Stack<Rect> _clipStack = new();
    private Rect _clip;
    private int _offsetX;
    private int _offsetY;

    public DrawingContext(TuiBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        _buffer = buffer;
        _clip = new Rect(0, 0, buffer.Width, buffer.Height);
        _clipStack.Push(_clip);
    }

    public void PushOffset(int x, int y)
    {
        _offsetX += x;
        _offsetY += y;
    }

    public void PopOffset(int x, int y)
    {
        _offsetX -= x;
        _offsetY -= y;
    }

    public void PushClip(Rect clip)
    {
        // World coordinates for the new clip
        var worldClip = new Rect(clip.X + _offsetX, clip.Y + _offsetY, clip.Width, clip.Height);
        
        // Intersect with current clip
        var currentClip = _clipStack.Peek();
        var x1 = Math.Max(worldClip.X, currentClip.X);
        var y1 = Math.Max(worldClip.Y, currentClip.Y);
        var x2 = Math.Min(worldClip.X + worldClip.Width, currentClip.X + currentClip.Width);
        var y2 = Math.Min(worldClip.Y + worldClip.Height, currentClip.Y + currentClip.Height);

        _clip = new Rect(x1, y1, Math.Max(0, x2 - x1), Math.Max(0, y2 - y1));
        _clipStack.Push(_clip);
    }

    public void PopClip()
    {
        _clipStack.Pop();
        _clip = _clipStack.Peek();
    }

    public void DrawChar(int x, int y, char c, TuiStyle style)
    {
        int worldX = _offsetX + x;
        int worldY = _offsetY + y;

        if (worldX >= _clip.X && worldX < _clip.X + _clip.Width &&
            worldY >= _clip.Y && worldY < _clip.Y + _clip.Height)
        {
            _buffer.SetCell(worldX, worldY, c, style);
        }
    }

    public void DrawString(int x, int y, string s, TuiStyle style)
    {
        ArgumentNullException.ThrowIfNull(s);
        for (int i = 0; i < s.Length; i++)
        {
            DrawChar(x + i, y, s[i], style);
        }
    }

    public void FillRectangle(Rect rect, char c, TuiStyle style)
    {
        for (int y = 0; y < rect.Height; y++)
        {
            for (int x = 0; x < rect.Width; x++)
            {
                DrawChar(rect.X + x, rect.Y + y, c, style);
            }
        }
    }
}
