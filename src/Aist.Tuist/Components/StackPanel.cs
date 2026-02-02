using Aist.Tuist.Primitives;

namespace Aist.Tuist.Components;

public class StackPanel : TuiElement
{
    public Orientation Orientation { get; set; } = Orientation.Vertical;

    protected override Size MeasureOverride(Size availableSize)
    {
        var size = Size.Zero;
        
        // When stacking, we allow infinite space in the stacking direction
        var childAvailableSize = Orientation == Orientation.Vertical 
            ? availableSize with { Height = int.MaxValue }
            : availableSize with { Width = int.MaxValue };

        foreach (var child in Children)
        {
            child.Measure(childAvailableSize);
            var childDesiredSize = child.DesiredSize;

            if (Orientation == Orientation.Vertical)
            {
                size = size with { 
                    Width = Math.Max(size.Width, childDesiredSize.Width),
                    Height = size.Height + childDesiredSize.Height
                };
            }
            else
            {
                size = size with { 
                    Width = size.Width + childDesiredSize.Width,
                    Height = Math.Max(size.Height, childDesiredSize.Height)
                };
            }
        }

        return size;
    }

    protected override void ArrangeOverride(Rect finalRect)
    {
        var offsetX = finalRect.X;
        var offsetY = finalRect.Y;
        
        foreach (var child in Children)
        {
            if (Orientation == Orientation.Vertical)
            {
                var childHeight = child.DesiredSize.Height;
                var childRect = new Rect(offsetX, offsetY, finalRect.Width, childHeight);
                child.Arrange(childRect);
                offsetY += childHeight;
            }
            else
            {
                var childWidth = child.DesiredSize.Width;
                var childRect = new Rect(offsetX, offsetY, childWidth, finalRect.Height);
                child.Arrange(childRect);
                offsetX += childWidth;
            }
        }
    }
}
