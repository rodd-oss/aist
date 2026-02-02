namespace Aist.Tuist.Primitives;

public readonly record struct Size(int Width, int Height)
{
    public static readonly Size Zero = new(0, 0);
    public static readonly Size Empty = new(0, 0);
    public static readonly Size Infinity = new(int.MaxValue, int.MaxValue);

    public override string ToString() => $"{Width}x{Height}";
}
