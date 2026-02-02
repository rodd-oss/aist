namespace Aist.Tuist.Events;

public enum RoutingStrategy
{
    Tunnel,
    Bubble,
    Direct
}

public class RoutedEventArgs : EventArgs
{
    public RoutedEventArgs(RoutedEvent routedEvent)
    {
        RoutedEvent = routedEvent;
    }

    public RoutedEvent RoutedEvent { get; }
    public object? Source { get; internal set; }
    public bool Handled { get; set; }
}
