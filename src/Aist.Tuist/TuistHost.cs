using Aist.Tuist.Events;
using Aist.Tuist.Focus;
using Aist.Tuist.Primitives;
using Aist.Tuist.Rendering;
using System.Collections.Concurrent;

namespace Aist.Tuist;

public sealed class TuistHost
{
    private readonly TuiBuffer _buffer;
    private readonly FocusManager _focusManager;
    private bool _shouldExit;
    private readonly ConcurrentQueue<Action> _dispatchQueue = new();

    public TuiElement? RootElement { get; set; }

    public event EventHandler? WindowResized;

    private const string AnsiEnterAlternateScreen = "\u001b[?1049h";
    private const string AnsiExitAlternateScreen = "\u001b[?1049l";
    private const string AnsiHideCursor = "\u001b[?25l";
    private const string AnsiShowCursor = "\u001b[?25h";
    
    public void Dispatch(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        _dispatchQueue.Enqueue(action);
    }

    public TuistHost()
    {
        _buffer = new TuiBuffer(Console.WindowWidth, Console.WindowHeight);
        _focusManager = new FocusManager();
    }

    public async Task RunAsync()
    {
        Console.Write(AnsiEnterAlternateScreen.ToCharArray());
        Console.Write(AnsiHideCursor.ToCharArray());

        try
        {
            while (!_shouldExit)
            {
                var width = Console.WindowWidth;
                var height = Console.WindowHeight;

                if (_buffer.Width != width || _buffer.Height != height)
                {
                    _buffer.Resize(width, height);
                    // Force a full clear on resize to prevent artifacts
                    Console.Clear();
                    WindowResized?.Invoke(this, EventArgs.Empty);
                }

                // Process dispatched actions (UI updates)
                while (_dispatchQueue.TryDequeue(out var action))
                {
                    action();
                }

                if (RootElement != null)
                {
                    // Input
                    ProcessInput();

                    // Layout
                    RootElement.Measure(new Primitives.Size(width, height));
                    RootElement.Arrange(new Rect(0, 0, width, height));

                    // Render
                    _buffer.Clear();
                    var context = new DrawingContext(_buffer);
                    RootElement.OnRender(context);
                    _buffer.Flush();
                }

                await Task.Delay(16).ConfigureAwait(false); // ~60 FPS
            }
        }
        finally
        {
            Console.Write(AnsiShowCursor.ToCharArray());
            Console.Write(AnsiExitAlternateScreen.ToCharArray());
        }
    }

    public void RequestExit() => _shouldExit = true;

    private void ProcessInput()
    {
        while (Console.KeyAvailable)
        {
            var keyInfo = Console.ReadKey(intercept: true);
            
            if (RootElement == null) continue;

            // Handle Tab for focus
            if (keyInfo.Key == ConsoleKey.Tab)
            {
                _focusManager.MoveFocus(RootElement, !keyInfo.Modifiers.HasFlag(ConsoleModifiers.Shift));
                continue;
            }

            // Dispatch KeyDown
            var args = new KeyRoutedEventArgs(
                TuiElement.KeyDownEvent,
                keyInfo.Key,
                keyInfo.KeyChar,
                keyInfo.Modifiers
            );

            var target = _focusManager.FocusedElement;
            
            // Validate that the focused element is still part of the active tree
            if (target != null && !IsDescendant(RootElement, target))
            {
                _focusManager.FocusedElement = null;
                target = null;
            }

            target ??= RootElement;
            target.DispatchEvent(args);
        }
    }

    private static bool IsDescendant(TuiElement root, TuiElement target)
    {
        var current = target;
        while (current != null)
        {
            if (current == root) return true;
            current = current.Parent;
        }
        return false;
    }
}
