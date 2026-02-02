using Aist.Tuist.Events;

namespace Aist.Tuist.Focus;

public class FocusManager
{
    private TuiElement? _focusedElement;

    public TuiElement? FocusedElement
    {
        get => _focusedElement;
        set
        {
            if (_focusedElement == value) return;

            var oldFocused = _focusedElement;
            if (oldFocused != null)
            {
                oldFocused.IsFocused = false;
                oldFocused.DispatchEvent(new RoutedEventArgs(TuiElement.LostFocusEvent));
            }

            _focusedElement = value;

            if (_focusedElement != null)
            {
                _focusedElement.IsFocused = true;
                _focusedElement.DispatchEvent(new RoutedEventArgs(TuiElement.GotFocusEvent));
            }
        }
    }

    public void MoveFocus(TuiElement root, bool forward)
    {
        ArgumentNullException.ThrowIfNull(root);
        var focusableElements = GetFocusableElements(root)
            .OrderBy(e => e.TabIndex)
            .ToList();

        if (focusableElements.Count == 0) return;

        int currentIndex = _focusedElement != null ? focusableElements.IndexOf(_focusedElement) : -1;

        int nextIndex;
        if (forward)
        {
            nextIndex = (currentIndex + 1) % focusableElements.Count;
        }
        else
        {
            nextIndex = (currentIndex - 1 + focusableElements.Count) % focusableElements.Count;
        }

        FocusedElement = focusableElements[nextIndex];
    }

    private static IEnumerable<TuiElement> GetFocusableElements(TuiElement element)
    {
        if (element.IsFocusable) yield return element;

        foreach (var child in element.Children)
        {
            foreach (var focusable in GetFocusableElements(child))
            {
                yield return focusable;
            }
        }
    }
}
