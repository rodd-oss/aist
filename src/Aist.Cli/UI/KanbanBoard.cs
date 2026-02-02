using Aist.Cli.Services;
using Aist.Shared;
using Spectre.Console;
using Spectre.Console.Rendering;
using System.Text;

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
    private ProjectResponse? _currentProject;
    private List<JobResponse> _jobs = [];
    private bool _shouldExit;
    
    private int _selectedColumn; // 0: Todo, 1: InProgress, 2: Done
    private int _selectedJobIndex;
    
    private readonly JobStatus[] _columnStatuses = [JobStatus.Todo, JobStatus.InProgress, JobStatus.Done];

    private TuiState _state = TuiState.Board;
    private readonly EditorContext _editorContext = new();
    private readonly TuiBuffer _buffer = new();

    public KanbanBoard(AistApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task RunAsync()
    {
        AnsiConsole.Write(new ControlCode("\x1b[?1049h"));
        EnableRawMode();
        
        try
        {
            while (!_shouldExit)
            {
                if (_currentProject != null && _jobs.Count == 0)
                {
                    await LoadJobsAsync().ConfigureAwait(false);
                }

                Render();
                await HandleInputAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            DisableRawMode();
            AnsiConsole.Write(new ControlCode("\x1b[?1049l"));
        }
    }

    private static void DisableRawMode()
    {
        AnsiConsole.Write(new ControlCode("\x1b[?1006l"));
        AnsiConsole.Write(new ControlCode("\x1b[?1000l"));
        while (Console.KeyAvailable) Console.ReadKey(true);
        AnsiConsole.Cursor.Show();
    }

    private static void EnableRawMode()
    {
        AnsiConsole.Write(new ControlCode("\x1b[?1000h"));
        AnsiConsole.Write(new ControlCode("\x1b[?1006h"));
        AnsiConsole.Cursor.Hide();
    }

    private async Task EnterProjectSelectionAsync()
    {
        var projects = await _apiClient.GetProjectsAsync().ConfigureAwait(false);
        _editorContext.Reset();
        if (projects != null)
        {
            _editorContext.Projects = projects;
        }
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

    private void Render()
    {
        var width = AnsiConsole.Console.Profile.Width;
        var height = AnsiConsole.Console.Profile.Height;
        
        _buffer.Resize(width, height);
        _buffer.Clear();
        
        var layout = new Layout("Root")
            .SplitRows(
                new Layout("Header").Size(2),
                new Layout("Main"),
                new Layout("Footer").Size(2)
            );

        bool isDimmed = _state != TuiState.Board;
        layout["Header"].Update(GetHeader(isDimmed));
        layout["Footer"].Update(GetFooter());
        layout["Main"].Update(GetColumns(isDimmed));

        // Draw background layout to buffer
        _buffer.Draw(layout, 0, 0, width, height);

        // If a dialog is active, draw it on top in the buffer
        if (isDimmed)
        {
            DrawDialogOverlayToBuffer(width, height);
        }

        // Only emit ANSI for changed cells
        _buffer.Flush();
    }

    private void DrawDialogOverlayToBuffer(int width, int height)
    {
        var content = RenderDialogContent();
        int dialogWidth = Math.Min(70, width - 4);
        
        // Measure content height dynamically using IRenderable interface
        var options = new RenderOptions(AnsiConsole.Console.Profile.Capabilities, new Size(dialogWidth, height));
        int dialogHeight = ((IRenderable)content).Measure(options, dialogWidth).Max + 4; // +4 for panel border and padding

        int left = (width - dialogWidth) / 2;
        int top = (height - dialogHeight) / 2;

        var panel = new Panel(content)
        {
            Border = BoxBorder.Double,
            BorderStyle = new Style(Color.Yellow),
            Padding = new Padding(2, 1, 2, 1),
            Header = new PanelHeader($"[bold yellow] {_state.ToString().ToUpper()} [/]"),
            Expand = false,
            Width = dialogWidth
        };

        _buffer.Draw(panel, left, top, dialogWidth, dialogHeight);
    }

    private Table RenderDialogContent()
    {
        if (_state == TuiState.SelectProject)
        {
            var table = new Table().Border(TableBorder.None).HideHeaders().Expand();
            table.AddColumn("Choice");

            for (int i = 0; i < _editorContext.Projects.Count; i++)
            {
                var p = _editorContext.Projects[i];
                var text = string.IsNullOrWhiteSpace(p.Title) ? "Untitled Project" : p.Title;
                if (i == _editorContext.SelectedProjectIndex)
                    table.AddRow($"[black on yellow] > {Markup.Escape(text)} [/]");
                else
                    table.AddRow($"  {Markup.Escape(text)}");
            }

            if (_editorContext.SelectedProjectIndex == _editorContext.Projects.Count)
                table.AddRow("[black on yellow] > + Create New Project [/]");
            else
                table.AddRow("  [green]+ Create New Project[/]");

            return table;
        }

        var inputTable = new Table().Border(TableBorder.None).Expand();
        inputTable.AddColumn(new TableColumn("Field").Width(15));
        inputTable.AddColumn("Input");

        switch (_state)
        {
            case TuiState.CreateJob:
            case TuiState.EditJob:
                inputTable.AddRow("Title", RenderField(_editorContext.Title, _editorContext.FocusedFieldIndex == 0));
                inputTable.AddRow("Description", RenderField(_editorContext.Description, _editorContext.FocusedFieldIndex == 1));
                inputTable.AddRow("Slug", RenderField(_editorContext.Slug, _editorContext.FocusedFieldIndex == 2));
                inputTable.AddRow("Type", RenderField(_editorContext.Type.ToString(), _editorContext.FocusedFieldIndex == 3));
                break;
            case TuiState.CreateProject:
                inputTable.AddRow("Title", RenderField(_editorContext.ProjectTitle, _editorContext.FocusedFieldIndex == 0));
                break;
        }
        return inputTable;
    }

    private Rows GetHeader(bool dimmed)
    {
        var headerText = _currentProject != null ? $"Aist Kanban: {_currentProject.Title}" : "Aist Kanban (No Project Selected)";
        var color = dimmed ? "grey" : "yellow";
        return new Rows(new Rule($"[{color}]{headerText}[/]") { Justification = Justify.Left }, Text.Empty);
    }

    private Rows GetFooter()
    {
        var footer = new Table().Border(TableBorder.None).HideHeaders().Expand();
        footer.AddColumn("Left");
        footer.AddColumn("Right");
        
        var hotkeys = _currentProject == null 
            ? "[grey]Select/New Project: [[P/N]]  Exit: [[Q]][/]"
            : "[grey]Navigate: [[Arrows]]  Move: [[T/I/D]]  New Job: [[J]]  Edit: [[E]]  Project: [[P/N]]  Refresh: [[R]]  Exit: [[Q]][/]";
            
        var selectedJob = (_jobs.Count > 0 && GetJobsInColumn(_selectedColumn).Count > _selectedJobIndex) 
            ? GetJobsInColumn(_selectedColumn)[_selectedJobIndex].ShortSlug 
            : "None";

        footer.AddRow(hotkeys, $"[grey]Selected: {Markup.Escape(selectedJob)}[/]");
        return new Rows(Text.Empty, footer);
    }

    private IRenderable GetColumns(bool dimmed)
    {
        if (_currentProject == null)
        {
            return new Panel(new Markup(dimmed ? "[grey]Press 'p' to select a project or 'q' to quit.[/]" : "Press [green]'p'[/] to select a project or [red]'q'[/] to quit."))
            {
                Border = BoxBorder.Rounded, Padding = new Padding(2, 1, 2, 1), Expand = true,
                BorderStyle = dimmed ? new Style(Color.Grey) : new Style(Color.White)
            };
        }

        return new Columns(
            CreateColumn("TODO", JobStatus.Todo, dimmed ? "grey" : "blue", _selectedColumn == 0, dimmed),
            CreateColumn("IN PROGRESS", JobStatus.InProgress, dimmed ? "grey" : "yellow", _selectedColumn == 1, dimmed),
            CreateColumn("DONE", JobStatus.Done, dimmed ? "grey" : "green", _selectedColumn == 2, dimmed)
        ).Expand();
    }

    private static string RenderField(string value, bool isFocused)
    {
        var escaped = Markup.Escape(value);
        return isFocused ? $"[black on yellow]{(string.IsNullOrEmpty(escaped) ? " " : escaped)}[/][blink yellow]_[/]" : escaped;
    }

    private Panel CreateColumn(string title, JobStatus status, string color, bool isSelectedColumn, bool dimmed)
    {
        var jobsInColumn = _jobs.Where(j => j.Status == status).ToList();
        var rowsList = new List<IRenderable>();

        for (int i = 0; i < jobsInColumn.Count; i++)
        {
            var job = jobsInColumn[i];
            bool isSelectedJob = !dimmed && isSelectedColumn && i == _selectedJobIndex;
            var cardContent = new StringBuilder();
            if (dimmed)
            {
                cardContent.Append($"[grey]{Markup.Escape(job.Title)}[/]\n");
                cardContent.Append($"[grey]{job.Type} • {Markup.Escape(job.ShortSlug)}[/]");
            }
            else
            {
                cardContent.Append($"[white]{Markup.Escape(job.Title)}[/]\n");
                cardContent.Append($"[grey]{job.Type} • {Markup.Escape(job.ShortSlug)}[/]");
            }
            
            rowsList.Add(new Panel(cardContent.ToString())
            {
                Border = isSelectedJob ? BoxBorder.Double : BoxBorder.Rounded,
                BorderStyle = dimmed ? new Style(Color.Grey) : (isSelectedJob ? new Style(Color.White) : new Style(Color.FromInt32(240))),
                Header = isSelectedJob ? new PanelHeader("[bold]>[/]") : null,
                Expand = true
            });
        }

        if (rowsList.Count == 0) rowsList.Add(new Text("No jobs", new Style(Color.Grey)));

        return new Panel(new Rows(rowsList))
        {
            Header = new PanelHeader($"[{color}]{title} ({jobsInColumn.Count})[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = !dimmed && isSelectedColumn ? new Style(Color.White) : new Style(Color.Grey),
            Expand = true, Padding = new Padding(1, 0, 1, 0)
        };
    }

    private async Task HandleInputAsync()
    {
        if (!Console.KeyAvailable) { await Task.Delay(50).ConfigureAwait(false); return; }
        var key = Console.ReadKey(true);
        if (_state == TuiState.Board) await HandleBoardInput(key).ConfigureAwait(false);
        else await HandleDialogInput(key).ConfigureAwait(false);
    }

    private async Task HandleBoardInput(ConsoleKeyInfo key)
    {
        if (key.Key == ConsoleKey.Escape)
        {
            var sb = new StringBuilder();
            while (Console.KeyAvailable) sb.Append(Console.ReadKey(true).KeyChar);
            var seq = sb.ToString();
            if (seq.StartsWith("[<", StringComparison.Ordinal)) { HandleMouseInput(seq); }
            return;
        }

        switch (key.Key)
        {
            case ConsoleKey.Q: _shouldExit = true; break;
            case ConsoleKey.R: await LoadJobsAsync().ConfigureAwait(false); break;
            case ConsoleKey.P: await EnterProjectSelectionAsync().ConfigureAwait(false); break;
            case ConsoleKey.N: _editorContext.Reset(); _state = TuiState.CreateProject; break;
            case ConsoleKey.J: if (_currentProject != null) { _editorContext.Reset(); _state = TuiState.CreateJob; } break;
            case ConsoleKey.E:
                var currentJobs = GetJobsInColumn(_selectedColumn);
                if (currentJobs.Count > 0 && _selectedJobIndex < currentJobs.Count)
                {
                    var job = currentJobs[_selectedJobIndex];
                    _editorContext.Reset();
                    _editorContext.Title = job.Title; _editorContext.Description = job.Description;
                    _editorContext.Slug = job.ShortSlug; _editorContext.Type = job.Type;
                    _editorContext.EditingJobId = job.Id; _state = TuiState.EditJob;
                }
                break;
            case ConsoleKey.LeftArrow: UpdateSelection(-1, 0); break;
            case ConsoleKey.RightArrow: UpdateSelection(1, 0); break;
            case ConsoleKey.UpArrow: UpdateSelection(0, -1); break;
            case ConsoleKey.DownArrow: UpdateSelection(0, 1); break;
            case ConsoleKey.Tab: UpdateSelection(1, 0); break;
            case ConsoleKey.T: await MoveSelectedJobAsync(JobStatus.Todo).ConfigureAwait(false); break;
            case ConsoleKey.I: await MoveSelectedJobAsync(JobStatus.InProgress).ConfigureAwait(false); break;
            case ConsoleKey.D: await MoveSelectedJobAsync(JobStatus.Done).ConfigureAwait(false); break;
        }
    }

    private void HandleMouseInput(string seq)
    {
        var match = System.Text.RegularExpressions.Regex.Match(seq, @"\[<(\d+);(\d+);(\d+)([Mm])");
        if (match.Success)
        {
            int btn = int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
            int x = int.Parse(match.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
            if (btn == 0)
            {
                var width = AnsiConsole.Console.Profile.Width;
                if (x < width / 3) _selectedColumn = 0;
                else if (x < 2 * width / 3) _selectedColumn = 1;
                else _selectedColumn = 2;
                _selectedJobIndex = 0;
            }
        }
    }

    private async Task HandleDialogInput(ConsoleKeyInfo key)
    {
        if (key.Key == ConsoleKey.Escape) { _state = TuiState.Board; return; }
        
        if (_state == TuiState.SelectProject)
        {
            int count = _editorContext.Projects.Count + 1; // +1 for "Create New Project"
            if (key.Key == ConsoleKey.UpArrow) _editorContext.SelectedProjectIndex = (_editorContext.SelectedProjectIndex - 1 + count) % count;
            else if (key.Key == ConsoleKey.DownArrow) _editorContext.SelectedProjectIndex = (_editorContext.SelectedProjectIndex + 1) % count;
            else if (key.Key == ConsoleKey.Enter) await HandleProjectSelectionAsync().ConfigureAwait(false);
            return;
        }

        if (key.Key == ConsoleKey.Tab) { _editorContext.FocusedFieldIndex = (_editorContext.FocusedFieldIndex + 1) % (_state == TuiState.CreateProject ? 1 : 4); return; }
        if (key.Key == ConsoleKey.Enter)
        {
            if (_state == TuiState.CreateProject) await SubmitCreateProjectAsync().ConfigureAwait(false);
            else if (_state == TuiState.CreateJob) await SubmitCreateJobAsync().ConfigureAwait(false);
            else if (_state == TuiState.EditJob) await SubmitEditJobAsync().ConfigureAwait(false);
            return;
        }
        if (key.Key == ConsoleKey.Backspace) { UpdateActiveField(null, true); return; }
        if (key.Key == ConsoleKey.UpArrow) { int max = _state == TuiState.CreateProject ? 1 : 4; _editorContext.FocusedFieldIndex = (_editorContext.FocusedFieldIndex - 1 + max) % max; return; }
        if (key.Key == ConsoleKey.DownArrow) { int max = _state == TuiState.CreateProject ? 1 : 4; _editorContext.FocusedFieldIndex = (_editorContext.FocusedFieldIndex + 1) % max; return; }

        if (!char.IsControl(key.KeyChar)) UpdateActiveField(key.KeyChar.ToString(), false);
    }

    private async Task HandleProjectSelectionAsync()
    {
        if (_editorContext.SelectedProjectIndex < _editorContext.Projects.Count)
        {
            _currentProject = _editorContext.Projects[_editorContext.SelectedProjectIndex];
            _jobs.Clear();
            _state = TuiState.Board;
        }
        else
        {
            _editorContext.Reset();
            _state = TuiState.CreateProject;
        }
    }

    private void UpdateActiveField(string? text, bool isBackspace)
    {
        if (_state == TuiState.CreateProject) _editorContext.ProjectTitle = ModifyString(_editorContext.ProjectTitle, text, isBackspace);
        else
        {
            switch (_editorContext.FocusedFieldIndex)
            {
                case 0: _editorContext.Title = ModifyString(_editorContext.Title, text, isBackspace); break;
                case 1: _editorContext.Description = ModifyString(_editorContext.Description, text, isBackspace); break;
                case 2: _editorContext.Slug = ModifyString(_editorContext.Slug, text, isBackspace); break;
                case 3: if (!isBackspace) { var types = Enum.GetValues<JobType>(); int idx = Array.IndexOf(types, _editorContext.Type); _editorContext.Type = types[(idx + 1) % types.Length]; } break;
            }
        }
    }

    private static string ModifyString(string original, string? append, bool isBackspace) => isBackspace ? (original.Length > 0 ? original[..^1] : original) : original + append;

    private async Task SubmitCreateProjectAsync()
    {
        if (string.IsNullOrWhiteSpace(_editorContext.ProjectTitle)) return;
        var project = await _apiClient.CreateProjectAsync(_editorContext.ProjectTitle).ConfigureAwait(false);
        if (project != null) { _currentProject = project; _jobs.Clear(); _state = TuiState.Board; }
    }

    private async Task SubmitCreateJobAsync()
    {
        if (_currentProject == null || string.IsNullOrWhiteSpace(_editorContext.Title)) return;
        var request = new CreateJobRequest(_currentProject.Id, _editorContext.Slug, _editorContext.Title, _editorContext.Type, _editorContext.Description);
        var job = await _apiClient.CreateJobAsync(request).ConfigureAwait(false);
        if (job != null) { await LoadJobsAsync().ConfigureAwait(false); _state = TuiState.Board; }
    }

    private async Task SubmitEditJobAsync()
    {
        if (_editorContext.EditingJobId == null) return;
        var request = new UpdateJobRequest(_editorContext.Slug, _editorContext.Title, _editorContext.Type, _editorContext.Description);
        var success = await _apiClient.UpdateJobAsync(_editorContext.EditingJobId.Value.ToString(), request).ConfigureAwait(false);
        if (success) { await LoadJobsAsync().ConfigureAwait(false); _state = TuiState.Board; }
    }

    private async Task MoveSelectedJobAsync(JobStatus newStatus)
    {
        var currentJobs = GetJobsInColumn(_selectedColumn);
        if (currentJobs.Count == 0 || _selectedJobIndex >= currentJobs.Count) return;
        var job = currentJobs[_selectedJobIndex];
        if (job.Status == newStatus) return;
        if (await _apiClient.UpdateJobStatusAsync(job.Id.ToString(), newStatus).ConfigureAwait(false)) await LoadJobsAsync().ConfigureAwait(false);
    }
}
