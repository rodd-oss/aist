namespace Aist.Tuist.Rendering;

public enum TuiColor
{
    Default = -1,
    Black = 0,
    Red = 1,
    Green = 2,
    Yellow = 3,
    Blue = 4,
    Magenta = 5,
    Cyan = 6,
    White = 7,
    BrightBlack = 8,
    BrightRed = 9,
    BrightGreen = 10,
    BrightYellow = 11,
    BrightBlue = 12,
    BrightMagenta = 13,
    BrightCyan = 14,
    BrightWhite = 15
}

public readonly record struct TuiStyle(TuiColor Foreground = TuiColor.Default, TuiColor Background = TuiColor.Default)
{
    public static readonly TuiStyle Default;
}
