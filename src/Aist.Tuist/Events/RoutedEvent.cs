namespace Aist.Tuist.Events;

public class RoutedEvent
{
    public string Name { get; }
    public RoutingStrategy RoutingStrategy { get; }
    public Type HandlerType { get; }

    public RoutedEvent(string name, RoutingStrategy routingStrategy, Type handlerType)
    {
        Name = name;
        RoutingStrategy = routingStrategy;
        HandlerType = handlerType;
    }
}
