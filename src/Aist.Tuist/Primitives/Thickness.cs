namespace Aist.Tuist.Primitives;

public readonly record struct Thickness(int Left, int Top, int Right, int Bottom)
{
    public Thickness(int uniformLength) : this(uniformLength, uniformLength, uniformLength, uniformLength) { }
    public Thickness(int leftRight, int topBottom) : this(leftRight, topBottom, leftRight, topBottom) { }

    public static readonly Thickness Zero = new(0);

    public int Horizontal => Left + Right;
    public int Vertical => Top + Bottom;

    public override string ToString() => $"({Left}, {Top}, {Right}, {Bottom})";
}
