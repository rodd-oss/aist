using Aist.Cli.Services;
using Aist.Core;
using Aist.Tuist;
using Aist.Tuist.Components;
using Aist.Tuist.Events;
using Aist.Tuist.Primitives;
using Aist.Tuist.Rendering;
using System.Text;
using HorizontalAlignment = Aist.Tuist.Primitives.HorizontalAlignment;
using VerticalAlignment = Aist.Tuist.Primitives.VerticalAlignment;

namespace Aist.Cli.UI;

internal enum TuiState
{
    Board,
    CreateProject,
    SelectProject,
    CreateJob,
    EditJob
}

internal sealed class EditorContext
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public JobType Type { get; set; } = JobType.Feature;
    public string ProjectTitle { get; set; } = string.Empty;
    public int FocusedFieldIndex { get; set; }
    public Guid? EditingJobId { get; set; }

    // Project Selection
    public List<ProjectResponse> Projects { get; set; } = [];
    public int SelectedProjectIndex { get; set; }

    public void Reset()
    {
        Title = string.Empty;
        Description = string.Empty;
        Slug = string.Empty;
        Type = JobType.Feature;
        ProjectTitle = string.Empty;
        FocusedFieldIndex = 0;
        EditingJobId = null;
        Projects.Clear();
        SelectedProjectIndex = 0;
    }
}

internal sealed class KanbanBoard
{
    private readonly AistApiClient _apiClient;
    private readonly TuistHost _host;
    private ProjectResponse? _currentProject;
    private List<JobResponse> _jobs = [];

    private int _selectedColumn; // 0: Todo, 1: InProgress, 2: Done
    private int _selectedJobIndex;

    private readonly JobStatus[] _columnStatuses = [JobStatus.Todo, JobStatus.InProgress, JobStatus.Done];

    private TuiState _state = TuiState.Board;
    private readonly EditorContext _editorContext = new();

    // Cached Input Controls
    private readonly TextBox _tbProjectTitle = new() { Width = 40 };
    private readonly TextBox _tbJobTitle = new() { Width = 40 };
    private readonly TextBox _tbJobDescription = new() { Width = 40 };
    private readonly TextBox _tbJobSlug = new() { Width = 40 };

    public KanbanBoard(AistApiClient apiClient)
    {
        _apiClient = apiClient;
        _host = new TuistHost();
        _host.WindowResized += (s, e) => RefreshUI();

        // Initialize controls
        _tbProjectTitle.IsFocusable = true;
        _tbJobTitle.IsFocusable = true;
        _tbJobDescription.IsFocusable = true;
        _tbJobSlug.IsFocusable = true;
    }

    public async Task RunAsync()
    {
        RefreshUI();

        // Initial load
        if (_currentProject != null)
        {
            _ = Task.Run(async () =>
            {
                await LoadJobsAsync().ConfigureAwait(false);
                RefreshUI();
            });
        }

        await _host.RunAsync().ConfigureAwait(false);
    }

    private void RefreshUI()
    {
        var root = new KanbanRoot();

        var mainStack = new StackPanel { Orientation = Orientation.Vertical, HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };

        // Header
        mainStack.Children.Add(new TextBlock
        {
            Text = GetHeaderText(),
            Foreground = TuiColor.Yellow,
            Margin = new Thickness(1, 0, 1, 1)
        });

        // Main
        if (_currentProject == null)
        {
            mainStack.Children.Add(new Border
            {
                Child = new TextBlock { Text = "Press 'p' to select a project or 'q' to quit.", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center },
                BorderStyle = BorderStyle.Rounded,
                Padding = new Thickness(2, 1),
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch
            });
        }
        else
        {
            var columnsStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Stretch, HorizontalAlignment = HorizontalAlignment.Stretch };

            // Calculate width accounting for margins (1 left + 1 right = 2 per column)
            var totalMargin = 3 * 2;
            var width = (Console.WindowWidth - totalMargin) / 3;

            // Calculate height explicitly to fill the screen (WindowHeight - Header(2) - Footer(2))
            var height = Math.Max(0, Console.WindowHeight - 4);
            columnsStack.Height = height;

            columnsStack.Children.Add(CreateColumn("TODO", JobStatus.Todo, TuiColor.Blue, _selectedColumn == 0, width));
            columnsStack.Children.Add(CreateColumn("IN PROGRESS", JobStatus.InProgress, TuiColor.Yellow, _selectedColumn == 1, width));
            columnsStack.Children.Add(CreateColumn("DONE", JobStatus.Done, TuiColor.Green, _selectedColumn == 2, width));
            mainStack.Children.Add(columnsStack);
        }

        // Footer
        mainStack.Children.Add(new TextBlock
        {
            Text = GetFooterText(),
            Foreground = TuiColor.BrightBlack,
            Margin = new Thickness(1, 1, 1, 0)
        });

        root.Children.Add(mainStack);

        var dialog = BuildDialog();
        if (dialog != null)
        {
            root.Children.Add(dialog);
        }

        root.KeyDown += OnKeyDown;
        _host.RootElement = root;
    }

    private Border CreateColumn(string title, JobStatus status, TuiColor color, bool isSelected, int width)
    {
        var jobs = _jobs.Where(j => j.Status == status).ToList();
        var border = new Border
        {
            Title = $"{title} ({jobs.Count})",
            BorderStyle = BorderStyle.Rounded,
            BorderStyleInfo = new TuiStyle(isSelected ? TuiColor.White : color),
            Width = width,
            VerticalAlignment = VerticalAlignment.Stretch,
            Margin = new Thickness(1, 0)
        };

        var stack = new StackPanel { Orientation = Orientation.Vertical, HorizontalAlignment = HorizontalAlignment.Stretch };
        for (int i = 0; i < jobs.Count; i++)
        {
            stack.Children.Add(CreateJobCard(jobs[i], isSelected && i == _selectedJobIndex));
        }

        if (jobs.Count == 0)
        {
            stack.Children.Add(new TextBlock { Text = "No jobs", Foreground = TuiColor.BrightBlack, Margin = new Thickness(1) });
        }

        border.Child = stack;
        return border;
    }

    private static Border CreateJobCard(JobResponse job, bool isSelected)
    {
        var border = new Border
        {
            BorderStyle = isSelected ? BorderStyle.DoubleLine : BorderStyle.SingleLine,
            BorderStyleInfo = new TuiStyle(isSelected ? TuiColor.White : TuiColor.BrightBlack),
            Padding = new Thickness(1, 0),
            Margin = new Thickness(0, 0, 0, 1),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var stack = new StackPanel { Orientation = Orientation.Vertical, HorizontalAlignment = HorizontalAlignment.Stretch };
        stack.Children.Add(new TextBlock { Text = job.Title, Foreground = TuiColor.White });
        stack.Children.Add(new TextBlock { Text = $"{job.Type} • {job.ShortSlug}", Foreground = TuiColor.BrightBlack });

        border.Child = stack;
        return border;
    }

    private string GetHeaderText()
    {
        return _currentProject != null ? $"Aist Kanban: {_currentProject.Title}" : "Aist Kanban (No Project Selected)";
    }

    private string GetFooterText()
    {
        var hotkeys = _currentProject == null
            ? "Select/New Project: [P/N]  Exit: [Q]"
            : "Navigate: [Arrows]  Move: [T/I/D]  New Job: [J]  Edit: [E]  Project: [P/N]  Refresh: [R]  Exit: [Q]";

        var currentColumnJobs = GetJobsInColumn(_selectedColumn);
        var selectedJob = (_jobs.Count > 0 && currentColumnJobs.Count > _selectedJobIndex)
            ? currentColumnJobs[_selectedJobIndex].ShortSlug
            : "None";

        return $"{hotkeys} | Selected: {selectedJob}";
    }

    private Border? BuildDialog()
    {
        if (_state == TuiState.Board) return null;

        var border = new Border
        {
            Title = _state.ToString().ToUpper(),
            BorderStyle = BorderStyle.DoubleLine,
            BorderStyleInfo = new TuiStyle(TuiColor.Yellow),
            Padding = new Thickness(2, 1),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        var stack = new StackPanel { Orientation = Orientation.Vertical, HorizontalAlignment = HorizontalAlignment.Stretch };

        if (_state == TuiState.SelectProject)
        {
            for (int i = 0; i < _editorContext.Projects.Count; i++)
            {
                var p = _editorContext.Projects[i];
                var text = (string.IsNullOrWhiteSpace(p.Title) ? "Untitled Project" : p.Title);
                stack.Children.Add(new TextBlock
                {
                    Text = (i == _editorContext.SelectedProjectIndex ? "> " : "  ") + text,
                    Foreground = i == _editorContext.SelectedProjectIndex ? TuiColor.Black : TuiColor.White,
                    Background = i == _editorContext.SelectedProjectIndex ? TuiColor.Yellow : TuiColor.Default
                });
            }
            var createNewIdx = _editorContext.Projects.Count;
            stack.Children.Add(new TextBlock
            {
                Text = (createNewIdx == _editorContext.SelectedProjectIndex ? "> " : "  ") + "+ Create New Project",
                Foreground = createNewIdx == _editorContext.SelectedProjectIndex ? TuiColor.Black : TuiColor.Green,
                Background = createNewIdx == _editorContext.SelectedProjectIndex ? TuiColor.Yellow : TuiColor.Default
            });
        }
        else
        {
            switch (_state)
            {
                case TuiState.CreateProject:
                    stack.Children.Add(new TextBlock { Text = "Project Title:" });
                    stack.Children.Add(_tbProjectTitle);
                    break;
                case TuiState.CreateJob:
                case TuiState.EditJob:
                    stack.Children.Add(new TextBlock { Text = "Title:" });
                    stack.Children.Add(_tbJobTitle);

                    stack.Children.Add(new TextBlock { Text = "Description:" });
                    stack.Children.Add(_tbJobDescription);

                    stack.Children.Add(new TextBlock { Text = "Slug:" });
                    stack.Children.Add(_tbJobSlug);

                    stack.Children.Add(new TextBlock { Text = "Type (Press Space to cycle):" });
                    stack.Children.Add(new TextBlock { Text = _editorContext.Type.ToString(), Foreground = TuiColor.Yellow });
                    break;
            }
        }

        border.Child = stack;
        return border;
    }

    private void OnKeyDown(object? sender, RoutedEventArgs e)
    {
        if (e is not KeyRoutedEventArgs ke) return;

        if (_state == TuiState.Board)
        {
            HandleBoardInput(ke);
        }
        else
        {
            HandleDialogInput(ke);
        }

        RefreshUI();
    }

    private void HandleBoardInput(KeyRoutedEventArgs ke)
    {
        switch (ke.Key)
        {
            case ConsoleKey.Q: _host.RequestExit(); break;
            case ConsoleKey.R: _ = Task.Run(async () => { await LoadJobsAsync().ConfigureAwait(false); _host.Dispatch(() => RefreshUI()); }); break;
            case ConsoleKey.P: _ = Task.Run(async () => { await EnterProjectSelectionAsync().ConfigureAwait(false); _host.Dispatch(() => RefreshUI()); }); break;
            case ConsoleKey.N:
                _editorContext.Reset();
                _tbProjectTitle.Text = string.Empty;
                _state = TuiState.CreateProject;
                break;
            case ConsoleKey.J:
                if (_currentProject != null)
                {
                    _editorContext.Reset();
                    _tbJobTitle.Text = string.Empty;
                    _tbJobDescription.Text = string.Empty;
                    _tbJobSlug.Text = string.Empty;
                    _state = TuiState.CreateJob;
                }
                break;
            case ConsoleKey.E:
                var currentJobs = GetJobsInColumn(_selectedColumn);
                if (currentJobs.Count > 0 && _selectedJobIndex < currentJobs.Count)
                {
                    var job = currentJobs[_selectedJobIndex];
                    _editorContext.Reset();
                    _editorContext.Title = job.Title;
                    _editorContext.Description = job.Description;
                    _editorContext.Slug = job.ShortSlug;
                    _editorContext.Type = job.Type;
                    _editorContext.EditingJobId = job.Id;

                    _tbJobTitle.Text = job.Title;
                    _tbJobDescription.Text = job.Description;
                    _tbJobSlug.Text = job.ShortSlug;

                    _state = TuiState.EditJob;
                }
                break;
            case ConsoleKey.LeftArrow: UpdateSelection(-1, 0); break;
            case ConsoleKey.RightArrow: UpdateSelection(1, 0); break;
            case ConsoleKey.UpArrow: UpdateSelection(0, -1); break;
            case ConsoleKey.DownArrow: UpdateSelection(0, 1); break;
            case ConsoleKey.T: _ = Task.Run(async () => { await MoveSelectedJobAsync(JobStatus.Todo).ConfigureAwait(false); await LoadJobsAsync().ConfigureAwait(false); _host.Dispatch(() => RefreshUI()); }); break;
            case ConsoleKey.I: _ = Task.Run(async () => { await MoveSelectedJobAsync(JobStatus.InProgress).ConfigureAwait(false); await LoadJobsAsync().ConfigureAwait(false); _host.Dispatch(() => RefreshUI()); }); break;
            case ConsoleKey.D: _ = Task.Run(async () => { await MoveSelectedJobAsync(JobStatus.Done).ConfigureAwait(false); await LoadJobsAsync().ConfigureAwait(false); _host.Dispatch(() => RefreshUI()); }); break;
        }
    }

    private void HandleDialogInput(KeyRoutedEventArgs ke)
    {
        if (ke.Key == ConsoleKey.Escape) { _state = TuiState.Board; return; }

        if (_state == TuiState.SelectProject)
        {
            int count = _editorContext.Projects.Count + 1;
            if (ke.Key == ConsoleKey.UpArrow) _editorContext.SelectedProjectIndex = (_editorContext.SelectedProjectIndex - 1 + count) % count;
            else if (ke.Key == ConsoleKey.DownArrow) _editorContext.SelectedProjectIndex = (_editorContext.SelectedProjectIndex + 1) % count;
            else if (ke.Key == ConsoleKey.Enter) _ = Task.Run(async () => { await HandleProjectSelectionAsync().ConfigureAwait(false); _host.Dispatch(() => RefreshUI()); });
            return;
        }

        if (ke.Key == ConsoleKey.Enter)
        {
            if (_state == TuiState.CreateProject) _ = Task.Run(async () => { await SubmitCreateProjectAsync().ConfigureAwait(false); });
            else if (_state == TuiState.CreateJob) _ = Task.Run(async () => { await SubmitCreateJobAsync().ConfigureAwait(false); });
            else if (_state == TuiState.EditJob) _ = Task.Run(async () => { await SubmitEditJobAsync().ConfigureAwait(false); });
        }

        if (!ke.Handled && ke.Key == ConsoleKey.Spacebar && (_state == TuiState.CreateJob || _state == TuiState.EditJob))
        {
            var types = Enum.GetValues<JobType>();
            int idx = Array.IndexOf(types, _editorContext.Type);
            _editorContext.Type = types[(idx + 1) % types.Length];
        }
    }

    private async Task EnterProjectSelectionAsync()
    {
        var projects = await _apiClient.GetProjectsAsync().ConfigureAwait(false);
        _editorContext.Reset();
        if (projects != null) _editorContext.Projects = projects;
        _state = TuiState.SelectProject;
    }

    private async Task LoadJobsAsync()
    {
        if (_currentProject == null) return;
        var jobs = await _apiClient.GetJobsAsync(_currentProject.Id.ToString()).ConfigureAwait(false);
        if (jobs != null)
        {
            _jobs = jobs;
            UpdateSelection(0, 0);
        }
    }

    private void UpdateSelection(int colDelta, int rowDelta)
    {
        _selectedColumn = Math.Clamp(_selectedColumn + colDelta, 0, 2);
        var currentColumnJobs = GetJobsInColumn(_selectedColumn);
        if (currentColumnJobs.Count == 0) _selectedJobIndex = 0;
        else _selectedJobIndex = Math.Clamp(_selectedJobIndex + rowDelta, 0, currentColumnJobs.Count - 1);
    }

    private List<JobResponse> GetJobsInColumn(int columnIndex)
    {
        var status = _columnStatuses[columnIndex];
        return _jobs.Where(j => j.Status == status).ToList();
    }

    private async Task HandleProjectSelectionAsync()
    {
        if (_editorContext.SelectedProjectIndex < _editorContext.Projects.Count)
        {
            _currentProject = _editorContext.Projects[_editorContext.SelectedProjectIndex];
            _jobs.Clear();
            _state = TuiState.Board;
            await LoadJobsAsync().ConfigureAwait(false);
        }
        else
        {
            _editorContext.Reset();
            _state = TuiState.CreateProject;
        }
    }

    private async Task SubmitCreateProjectAsync()
    {
        var title = _tbProjectTitle.Text;
        if (string.IsNullOrWhiteSpace(title)) return;
        var project = await _apiClient.CreateProjectAsync(title).ConfigureAwait(false);
        if (project != null)
        {
            _currentProject = project;
            _jobs.Clear();
            _state = TuiState.Board;
            await LoadJobsAsync().ConfigureAwait(false);
            _host.Dispatch(() => RefreshUI());
        }
    }

    private async Task SubmitCreateJobAsync()
    {
        var title = _tbJobTitle.Text;
        if (_currentProject == null || string.IsNullOrWhiteSpace(title)) return;
        var request = new CreateJobRequest(_currentProject.Id, _tbJobSlug.Text, title, _editorContext.Type, _tbJobDescription.Text);
        var job = await _apiClient.CreateJobAsync(request).ConfigureAwait(false);
        if (job != null)
        {
            await LoadJobsAsync().ConfigureAwait(false);
            _state = TuiState.Board;
            _host.Dispatch(() => RefreshUI());
        }
    }

    private async Task SubmitEditJobAsync()
    {
        if (_editorContext.EditingJobId == null) return;
        var request = new UpdateJobRequest(_tbJobSlug.Text, _tbJobTitle.Text, _editorContext.Type, _tbJobDescription.Text);
        var success = await _apiClient.UpdateJobAsync(_editorContext.EditingJobId.Value.ToString(), request).ConfigureAwait(false);
        if (success)
        {
            await LoadJobsAsync().ConfigureAwait(false);
            _state = TuiState.Board;
            _host.Dispatch(() => RefreshUI());
        }
    }

    private async Task MoveSelectedJobAsync(JobStatus newStatus)
    {
        var currentJobs = GetJobsInColumn(_selectedColumn);
        if (currentJobs.Count == 0 || _selectedJobIndex >= currentJobs.Count) return;
        var job = currentJobs[_selectedJobIndex];
        if (job.Status == newStatus) return;
        await _apiClient.UpdateJobStatusAsync(job.Id.ToString(), newStatus).ConfigureAwait(false);
    }
}

internal sealed class KanbanRoot : TuiElement
{
    protected override Aist.Tuist.Primitives.Size MeasureOverride(Aist.Tuist.Primitives.Size availableSize)
    {
        foreach (var child in Children) child.Measure(availableSize);
        return availableSize;
    }

    protected override void ArrangeOverride(Rect finalRect)
    {
        if (Children.Count > 0) Children[0].Arrange(finalRect); // Main content
        if (Children.Count > 1) // Dialog
        {
            var dialog = Children[1];
            var size = dialog.DesiredSize;
            var x = (finalRect.Width - size.Width) / 2;
            var y = (finalRect.Height - size.Height) / 2;
            dialog.Arrange(new Rect(x, y, size.Width, size.Height));
        }
    }
}
