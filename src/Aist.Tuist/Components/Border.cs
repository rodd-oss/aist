using Aist.Tuist.Primitives;
using Aist.Tuist.Rendering;

namespace Aist.Tuist.Components;

public class Border : TuiElement
{
    public BorderStyle BorderStyle { get; set; } = BorderStyle.SingleLine;
    public string? Title { get; set; }
    public TuiStyle BorderStyleInfo { get; set; } = TuiStyle.Default;

    public TuiElement? Child
    {
        get => Children.Count > 0 ? Children[0] : null;
        set
        {
            Children.Clear();
            if (value != null) Children.Add(value);
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var borderThickness = BorderStyle == BorderStyle.None ? 0 : 1;
        var horizontalBorder = borderThickness * 2;
        var verticalBorder = borderThickness * 2;

        var innerAvailableSize = new Size(
            Math.Max(0, availableSize.Width - horizontalBorder),
            Math.Max(0, availableSize.Height - verticalBorder)
        );

        if (Child != null)
        {
            Child.Measure(innerAvailableSize);
            return new Size(
                Child.DesiredSize.Width + horizontalBorder,
                Child.DesiredSize.Height + verticalBorder
            );
        }

        return new Size(horizontalBorder, verticalBorder);
    }

    protected override void ArrangeOverride(Rect finalRect)
    {
        // finalRect is already inside the padding
        var borderThickness = BorderStyle == BorderStyle.None ? 0 : 1;

        if (Child != null)
        {
            var childRect = new Rect(
                finalRect.X + borderThickness,
                finalRect.Y + borderThickness,
                Math.Max(0, finalRect.Width - (borderThickness * 2)),
                Math.Max(0, finalRect.Height - (borderThickness * 2))
            );
            Child.Arrange(childRect);
        }
    }

    public override void OnRender(DrawingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        
        // Fill background to ensure opacity (prevent see-through)
        var width = ActualBounds.Width;
        var height = ActualBounds.Height;
        if (width > 0 && height > 0)
        {
            // Use the BorderStyleInfo's background if set, otherwise default
            var bg = BorderStyleInfo.Background == TuiColor.Default ? TuiColor.Default : BorderStyleInfo.Background;
            context.FillRectangle(new Rect(0, 0, width, height), ' ', new TuiStyle(TuiColor.Default, bg));
        }

        if (BorderStyle != BorderStyle.None)
        {
            DrawBorder(context);
        }
        base.OnRender(context);
    }

    private void DrawBorder(DrawingContext context)
    {
        var (tl, tr, bl, br, h, v) = GetBoxChars();
        var width = ActualBounds.Width;
        var height = ActualBounds.Height;

        if (width <= 0 || height <= 0) return;

        // Corners
        context.DrawChar(0, 0, tl, BorderStyleInfo);
        if (width > 1) context.DrawChar(width - 1, 0, tr, BorderStyleInfo);
        if (height > 1) context.DrawChar(0, height - 1, bl, BorderStyleInfo);
        if (width > 1 && height > 1) context.DrawChar(width - 1, height - 1, br, BorderStyleInfo);

        // Horizontal lines
        for (int x = 1; x < width - 1; x++)
        {
            context.DrawChar(x, 0, h, BorderStyleInfo);
            if (height > 1) context.DrawChar(x, height - 1, h, BorderStyleInfo);
        }

        // Vertical lines
        for (int y = 1; y < height - 1; y++)
        {
            context.DrawChar(0, y, v, BorderStyleInfo);
            if (width > 1) context.DrawChar(width - 1, y, v, BorderStyleInfo);
        }

        // Title
        if (!string.IsNullOrEmpty(Title) && width > 4)
        {
            var titleText = $" {Title} ";
            var maxLength = width - 4; // Padding for corners
            if (titleText.Length > maxLength) titleText = titleText.Substring(0, maxLength);
            context.DrawString(2, 0, titleText, BorderStyleInfo);
        }
    }

    private (char tl, char tr, char bl, char br, char h, char v) GetBoxChars()
    {
        return BorderStyle switch
        {
            BorderStyle.DoubleLine => ('╔', '╗', '╚', '╝', '═', '║'),
            BorderStyle.Rounded => ('╭', '╮', '╰', '╯', '─', '│'),
            _ => ('┌', '┐', '└', '┘', '─', '│')
        };
    }
}
