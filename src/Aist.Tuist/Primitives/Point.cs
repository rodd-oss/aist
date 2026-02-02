namespace Aist.Tuist.Primitives;

public readonly record struct Point(int X, int Y)
{
    public static readonly Point Zero = new(0, 0);

    public override string ToString() => $"({X}, {Y})";
}
