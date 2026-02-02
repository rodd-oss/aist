using System.Collections.ObjectModel;
using Aist.Tuist.Primitives;
using Aist.Tuist.Rendering;
using Aist.Tuist.Events;

namespace Aist.Tuist;

public abstract class TuiElement
{
    private TuiElement? _parent;

    public TuiElement? Parent
    {
        get => _parent;
        internal set => _parent = value;
    }

    public Thickness Margin { get; set; }
    public Thickness Padding { get; set; }
    public HorizontalAlignment HorizontalAlignment { get; set; } = HorizontalAlignment.Stretch;
    public VerticalAlignment VerticalAlignment { get; set; } = VerticalAlignment.Stretch;

    public int? Width { get; set; }
    public int? Height { get; set; }

    public bool IsFocusable { get; set; }
    public int TabIndex { get; set; } = int.MaxValue;
    public bool IsFocused { get; internal set; }
    public bool IsMouseOver { get; internal set; }

    public TuiElementCollection Children { get; }

    protected TuiElement()
    {
        Children = new TuiElementCollection(this);
    }

    public Size DesiredSize { get; private set; }
    public Rect ActualBounds { get; private set; }

    // Event handlers
    private readonly Dictionary<RoutedEvent, List<EventHandler<RoutedEventArgs>>> _eventHandlers = new();

    public void AddHandler(RoutedEvent routedEvent, EventHandler<RoutedEventArgs> handler)
    {
        if (!_eventHandlers.TryGetValue(routedEvent, out var handlers))
        {
            handlers = new List<EventHandler<RoutedEventArgs>>();
            _eventHandlers[routedEvent] = handlers;
        }
        handlers.Add(handler);
    }

    public void RemoveHandler(RoutedEvent routedEvent, EventHandler<RoutedEventArgs> handler)
    {
        if (_eventHandlers.TryGetValue(routedEvent, out var handlers))
        {
            handlers.Remove(handler);
        }
    }

    public void DispatchEvent(RoutedEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);
        args.Source = this;

        if (args.RoutedEvent.RoutingStrategy == RoutingStrategy.Direct)
        {
            InvokeHandler(this, args);
        }
        else if (args.RoutedEvent.RoutingStrategy == RoutingStrategy.Tunnel)
        {
            var path = GetPathToRoot();
            for (int i = path.Count - 1; i >= 0; i--)
            {
                InvokeHandler(path[i], args);
                if (args.Handled) break;
            }
        }
        else if (args.RoutedEvent.RoutingStrategy == RoutingStrategy.Bubble)
        {
            var current = this;
            while (current != null)
            {
                InvokeHandler(current, args);
                if (args.Handled) break;
                current = current.Parent;
            }
        }
    }

    private List<TuiElement> GetPathToRoot()
    {
        var path = new List<TuiElement>();
        var current = this;
        while (current != null)
        {
            path.Add(current);
            current = current.Parent;
        }
        return path;
    }

    private static void InvokeHandler(TuiElement element, RoutedEventArgs args)
    {
        if (element._eventHandlers.TryGetValue(args.RoutedEvent, out var handlers))
        {
            foreach (var handler in handlers.ToList())
            {
                handler(element, args);
            }
        }
    }

    // Standard Events
    public static readonly RoutedEvent KeyDownEvent = new("KeyDown", RoutingStrategy.Bubble, typeof(EventHandler<RoutedEventArgs>));
    public static readonly RoutedEvent MouseDownEvent = new("MouseDown", RoutingStrategy.Bubble, typeof(EventHandler<RoutedEventArgs>));
    public static readonly RoutedEvent MouseEnterEvent = new("MouseEnter", RoutingStrategy.Direct, typeof(EventHandler<RoutedEventArgs>));
    public static readonly RoutedEvent MouseLeaveEvent = new("MouseLeave", RoutingStrategy.Direct, typeof(EventHandler<RoutedEventArgs>));
    public static readonly RoutedEvent GotFocusEvent = new("GotFocus", RoutingStrategy.Bubble, typeof(EventHandler<RoutedEventArgs>));
    public static readonly RoutedEvent LostFocusEvent = new("LostFocus", RoutingStrategy.Bubble, typeof(EventHandler<RoutedEventArgs>));

    public event EventHandler<RoutedEventArgs> KeyDown { add => AddHandler(KeyDownEvent, value); remove => RemoveHandler(KeyDownEvent, value); }
    public event EventHandler<RoutedEventArgs> MouseDown { add => AddHandler(MouseDownEvent, value); remove => RemoveHandler(MouseDownEvent, value); }
    public event EventHandler<RoutedEventArgs> MouseEnter { add => AddHandler(MouseEnterEvent, value); remove => RemoveHandler(MouseEnterEvent, value); }
    public event EventHandler<RoutedEventArgs> MouseLeave { add => AddHandler(MouseLeaveEvent, value); remove => RemoveHandler(MouseLeaveEvent, value); }
    public event EventHandler<RoutedEventArgs> GotFocus { add => AddHandler(GotFocusEvent, value); remove => RemoveHandler(GotFocusEvent, value); }
    public event EventHandler<RoutedEventArgs> LostFocus { add => AddHandler(LostFocusEvent, value); remove => RemoveHandler(LostFocusEvent, value); }


    public void Measure(Size availableSize)
    {
        // Apply Margin
        var margin = Margin;
        var padding = Padding;
        var constrainedSize = new Size(
            Math.Max(0, availableSize.Width - margin.Horizontal - padding.Horizontal),
            Math.Max(0, availableSize.Height - margin.Vertical - padding.Vertical)
        );

        // Apply explicit Width/Height
        if (Width.HasValue) constrainedSize = constrainedSize with { Width = Math.Min(constrainedSize.Width, Width.Value) };
        if (Height.HasValue) constrainedSize = constrainedSize with { Height = Math.Min(constrainedSize.Height, Height.Value) };

        var desiredSize = MeasureOverride(constrainedSize);

        // Re-apply explicit Width/Height to DesiredSize if set
        if (Width.HasValue) desiredSize = desiredSize with { Width = Width.Value };
        if (Height.HasValue) desiredSize = desiredSize with { Height = Height.Value };

        DesiredSize = new Size(
            desiredSize.Width + margin.Horizontal + padding.Horizontal,
            desiredSize.Height + margin.Vertical + padding.Vertical
        );
    }

    protected virtual Size MeasureOverride(Size availableSize)
    {
        return Size.Zero;
    }

    public void Arrange(Rect finalRect)
    {
        var margin = Margin;
        var padding = Padding;
        var arrangeRect = new Rect(
            finalRect.X + margin.Left,
            finalRect.Y + margin.Top,
            Math.Max(0, finalRect.Width - margin.Horizontal),
            Math.Max(0, finalRect.Height - margin.Vertical)
        );

        // Alignment
        if (HorizontalAlignment != HorizontalAlignment.Stretch)
        {
            arrangeRect = arrangeRect with { Width = Math.Min(arrangeRect.Width, DesiredSize.Width - margin.Horizontal) };
            if (HorizontalAlignment == HorizontalAlignment.Center)
                arrangeRect = arrangeRect with { X = arrangeRect.X + (finalRect.Width - margin.Horizontal - arrangeRect.Width) / 2 };
            else if (HorizontalAlignment == HorizontalAlignment.Right)
                arrangeRect = arrangeRect with { X = arrangeRect.X + (finalRect.Width - margin.Horizontal - arrangeRect.Width) };
        }

        if (VerticalAlignment != VerticalAlignment.Stretch)
        {
            arrangeRect = arrangeRect with { Height = Math.Min(arrangeRect.Height, DesiredSize.Height - margin.Vertical) };
            if (VerticalAlignment == VerticalAlignment.Center)
                arrangeRect = arrangeRect with { Y = arrangeRect.Y + (finalRect.Height - margin.Vertical - arrangeRect.Height) / 2 };
            else if (VerticalAlignment == VerticalAlignment.Bottom)
                arrangeRect = arrangeRect with { Y = arrangeRect.Y + (finalRect.Height - margin.Vertical - arrangeRect.Height) };
        }

        ActualBounds = arrangeRect;
        
        // Pass the rect inside the padding to ArrangeOverride
        var innerRect = new Rect(
            padding.Left,
            padding.Top,
            Math.Max(0, arrangeRect.Width - padding.Horizontal),
            Math.Max(0, arrangeRect.Height - padding.Vertical)
        );
        ArrangeOverride(innerRect);
    }

    protected virtual void ArrangeOverride(Rect finalRect)
    {
    }

    public virtual void OnRender(DrawingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        foreach (var child in Children)
        {
            context.PushOffset(child.ActualBounds.X, child.ActualBounds.Y);
            child.OnRender(context);
            context.PopOffset(child.ActualBounds.X, child.ActualBounds.Y);
        }
    }
}
