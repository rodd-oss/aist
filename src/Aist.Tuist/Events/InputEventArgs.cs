using Aist.Tuist.Primitives;

namespace Aist.Tuist.Events;

public class KeyRoutedEventArgs : RoutedEventArgs
{
    public KeyRoutedEventArgs(RoutedEvent routedEvent, ConsoleKey key, char keyChar, ConsoleModifiers modifiers) 
        : base(routedEvent)
    {
        Key = key;
        KeyChar = keyChar;
        Modifiers = modifiers;
    }

    public ConsoleKey Key { get; }
    public char KeyChar { get; }
    public ConsoleModifiers Modifiers { get; }
}

public class MouseRoutedEventArgs : RoutedEventArgs
{
    public MouseRoutedEventArgs(RoutedEvent routedEvent, Point position) 
        : base(routedEvent)
    {
        Position = position;
    }

    public Point Position { get; }
}
