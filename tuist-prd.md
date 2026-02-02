# PRD: Tuist TUI Rendering Engine

## 1. Overview
`tuist` is a modern, tree-based terminal user interface (TUI) framework designed for the `aist` CLI. It replaces imperative drawing with a declarative component model, enabling complex, interactive, and visually polished terminal applications.

## 2. Goals
- **Declarative UI:** Define UI using a tree of components rather than manual drawing commands.
- **Advanced Layout:** Implement a robust layout engine capable of handling dynamic resizing and complex nested structures.
- **Pixel Perfection:** Provide direct control over every character and style in the terminal buffer.
- **Interactive Consistency:** Standardize how input, focus, and events are handled across all UI elements.

## 3. Core Features

### 3.1 Layout Engine (Measure & Arrange)
The engine will follow the two-pass layout pattern:
- **Measure Pass:** Elements determine their desired size based on constraints provided by their parents and the requirements of their children.
- **Arrange Pass:** Parents assign final coordinates and dimensions (ActualBounds) to their children.
- **Alignment & Sizing:** Support for `Margin`, `Padding`, `HorizontalAlignment`, `VerticalAlignment`, `Stretch`, and explicit `Width`/`Height` constraints.

### 3.2 Rendering Engine
- **Direct Buffer Manipulation:** Moving away from third-party layout engines (like Spectre.Console) to directly manipulate a double-buffered grid of cells.
- **DrawingContext:** A coordinate-aware abstraction for components to draw primitives (text, lines, boxes, borders).
- **Clipping:** Automatic content clipping to ensure child elements do not render outside their parent's assigned bounds.
- **Double Buffering:** Efficient diffing between front and back buffers to minimize ANSI escape sequence output.

### 3.3 Event System (DOM-style)
- **Routed Events:** Support for event propagation:
    - **Tunneling:** Events travel from the root down to the target (useful for preview/interception).
    - **Bubbling:** Events travel from the target up to the root (standard event handling).
- **Focus Management:** A centralized system to manage keyboard focus, tab navigation, and focus-dependent styling.
- **Input Mapping:** High-level events (Click, KeyDown, FocusChanged) mapped from low-level terminal input.

## 4. Component Library (Phase 1)
| Component | Purpose |
| :--- | :--- |
| **View/StackPanel** | A container that stacks children vertically or horizontally. |
| **Border** | Adds a decorative frame and optional title around a child element. |
| **TextBlock** | Renders styled text with support for wrapping and alignment. |
| **Button** | A clickable element with hover, focus, and active states. |
| **TextBox** | An interactive single-line or multi-line text input field. |
| **ScrollViewer** | A container that provides a viewport into content larger than itself. |
| **ListBox** | A selectable list of items with scroll support. |

## 5. Technical Requirements
- **Language:** C# / .NET
- **Namespace:** `Aist.Tuist`
- **Performance:** Layout and rendering must remain fluid (target 30+ FPS) even with complex trees.
- **Zero-Dependency Layout:** The layout logic must not depend on Spectre.Console's `Layout` or `Table` classes.

## 6. Success Criteria
- Successful refactor of the existing `KanbanBoard` to use `tuist` components.
- Ability to create custom components by inheriting from `TuiElement`.
- Fluid resizing of the terminal window without flickering or layout breakage.
- Fully functional keyboard navigation (Tab, Arrows, Enter) across all interactive elements.
