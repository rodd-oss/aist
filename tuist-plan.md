# Tuist TUI Engine Implementation Plan

Goal: Transition from Spectre.Console-based imperative rendering to a declarative, tree-based UI framework (`tuist`) with a WPF-style layout engine and DOM-style event bubbling.

## 1. Core Architecture (Measure/Arrange)
Implement the two-pass layout system to handle complex nesting and automatic sizing.

### 1.1 Layout Primitives
- `Point`, `Size`, `Rect`: Basic geometric structures.
- `Thickness`: Support for Margin and Padding.
- `HorizontalAlignment` / `VerticalAlignment`: Stretch, Center, Start, End.

### 1.2 `TuiElement` Base Class
- `DesiredSize`: Calculated during Measure pass.
- `ActualBounds`: Final rendered position and size after Arrange pass.
- `Measure(Size availableSize)`: Standard WPF-style entry point.
- `Arrange(Rect finalRect)`: Standard WPF-style placement.
- `OnRender(DrawingContext context)`: Direct buffer manipulation hook.

## 2. Rendering Engine (Direct Buffer)
Decouple from Spectre.Console to allow pixel-perfect control.

### 2.1 `TuiBuffer` Refactor
- Remove `IRenderable` dependency for core drawing.
- Implement `SetCell(int x, int y, char c, Style style)`.
- Implement `DrawText`, `DrawLine`, `DrawRect`.
- Optimize `Flush()` for minimal ANSI output (keeping the existing double-buffer logic).

### 2.2 `DrawingContext`
- Handles coordinate translation (relative to element → global terminal coordinates).
- Implements clipping to ensure children don't draw outside parent bounds.

## 3. Event System (DOM-style Bubbling)
Enable interactive components with a robust event propagation model.

### 3.1 `RoutedEvent`
- Support for **Tunneling** (Preview events: Root → Target).
- Support for **Bubbling** (Standard events: Target → Root).
- `Handled` flag to stop propagation.

### 3.2 Focus Management
- `FocusManager` to track the active element.
- Automatic Tab/Shift+Tab navigation based on the component tree.

## 4. Milestone Roadmap

### Milestone 1: Foundation
- Create `Aist.Tuist` project in `src/Aist.Tuist/`.
- Implement primitives and `TuiElement`.
- Implement `TuiBuffer` for direct manipulation.

### Milestone 2: Layout & Basic Components
- Implement `StackPanel` (Horizontal/Vertical).
- Implement `Border` (Container with frames).
- Implement `TextBlock` (Text rendering with wrapping).

### Milestone 3: Interaction
- Implement `Button` with Click events.
- Implement `TextBox` with cursor and text state.
- Implement `ScrollViewer` for content clipping and offsets.

### Milestone 4: Integration
- Create `TuistHost` to run the lifecycle loop (Input -> Layout -> Render -> Flush).
- Refactor `KanbanBoard.cs` to use the new component-based architecture.
- Remove legacy Spectre.Console layout code.
