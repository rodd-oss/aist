namespace Aist.Tuist.Primitives;

public readonly record struct Rect(int X, int Y, int Width, int Height)
{
    public static readonly Rect Empty = new(0, 0, 0, 0);

    public int Left => X;
    public int Top => Y;
    public int Right => X + Width;
    public int Bottom => Y + Height;

    public Point Location => new(X, Y);
    public Size Size => new(Width, Height);

    public bool Contains(int x, int y)
    {
        return x >= X && x < X + Width && y >= Y && y < Y + Height;
    }

    public bool Contains(Point point) => Contains(point.X, point.Y);

    public override string ToString() => $"({X}, {Y}, {Width}, {Height})";
}
