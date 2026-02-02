using Aist.Tuist.Primitives;
using Aist.Tuist.Rendering;

namespace Aist.Tuist.Components;

public class ScrollViewer : TuiElement
{
    public Point ScrollOffset { get; set; }

    public TuiElement? Content
    {
        get => Children.Count > 0 ? Children[0] : null;
        set
        {
            Children.Clear();
            if (value != null)
            {
                Children.Add(value);
            }
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Content != null)
        {
            Content.Measure(new Size(1000, 1000)); // Infinite measure for content
            return new Size(
                Math.Min(availableSize.Width, Content.DesiredSize.Width),
                Math.Min(availableSize.Height, Content.DesiredSize.Height)
            );
        }
        return Size.Zero;
    }

    protected override void ArrangeOverride(Rect finalRect)
    {
        if (Content != null)
        {
            Content.Arrange(new Rect(-ScrollOffset.X, -ScrollOffset.Y, Content.DesiredSize.Width, Content.DesiredSize.Height));
        }
    }

    public override void OnRender(DrawingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        
        context.PushClip(new Rect(0, 0, ActualBounds.Width, ActualBounds.Height));
        base.OnRender(context);
        context.PopClip();
    }
}
