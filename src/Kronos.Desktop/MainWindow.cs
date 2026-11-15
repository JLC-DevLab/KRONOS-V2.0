// Copyright (c) 2026 José Luis Cabañas (JLC / UlysesX12).
// Distribuido bajo Volatility Software License 1.0. Véase LICENSE.md.
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Kronos.Core;

namespace Kronos.Desktop;

public sealed partial class MainWindow : Window
{
    private CaseService? current;
    private readonly TextBox caseId = new() { Watermark = "Nombre del caso", Width = 250 };
    private readonly TextBox operatorName = new() { Watermark = "Nombre del analista forense", Width = 280 };
    private readonly TextBox search = new() { Watermark = "Buscar por nombre, ruta o SHA-256…" };
    private readonly TextBox notes = new() { AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 250, Watermark = "Hallazgos, fuentes y conclusiones revisadas por el profesional…" };
    private readonly TextBox results = new() { IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 250 };
    private readonly ListBox evidenceList = new() { MinHeight = 200 };
    private readonly TextBlock title = new() { Text = "Ningún caso abierto", FontSize = 22, FontWeight = FontWeight.SemiBold };
    private readonly TextBlock summary = new() { Text = "Crea un caso o abre un archivo case.kronos.json para comenzar.", TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock status = new() { Text = "Preparado · KRONOS V2.0", TextWrapping = TextWrapping.Wrap, MaxHeight = 65 };
    private readonly ProgressBar progress = new() { IsIndeterminate = true, IsVisible = false, Height = 3 };
    private readonly CheckBox consent = new() { Content = "Autorizo capturar datos del equipo actual (sin RAM).", IsChecked = false };
    private readonly TextBox python = new() { Text = DefaultPython(), Watermark = "Ruta del intérprete Python 3" };
    private readonly List<Button> buttons = [];
    private TabControl navigation = null!;
    private readonly TextBlock caseLocation = new() { Text = "Aún no has abierto un caso.", TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock caseOverview = new() { Text = "Empieza creando un caso o abre uno existente.", TextWrapping = TextWrapping.Wrap };
    private bool busy;
    private CancellationTokenSource? operationCancellation;
    private string? completionMessage;

    public MainWindow(CaseService? initialCase = null)
    {
        Title = "KRONOS V2.0 — Laboratorio forense";
        Width = 1280; Height = 850; MinWidth = 1000; MinHeight = 700; WindowState = WindowState.Maximized;
        Background = Brush.Parse("#F3F6FA");
        var header = new StackPanel { Spacing = 5, Children = {
            new TextBlock { Text = "KRONOS / V2.0   ·   Laboratorio forense", FontSize = 24, FontWeight = FontWeight.Bold, Foreground = Brush.Parse("#174B70") },
            new TextBlock { Text = "Kit de Recolección, Organización y Normalización de Observaciones de Seguridad", Foreground = Brush.Parse("#536579"), FontSize = 12, TextWrapping = TextWrapping.Wrap },
            new TextBlock { Text = "Developed By JLC 2026 UlysesX12", Foreground = Brush.Parse("#536579"), FontSize = 11 }
        }};
        var createBar = new WrapPanel { Orientation = Orientation.Horizontal };
        createBar.Children.Add(new StackPanel { Spacing = 5, Margin = new Thickness(0,0,12,0), Children = { new TextBlock { Text = "Nombre del caso" }, caseId } });
        createBar.Children.Add(new StackPanel { Spacing = 5, Children = { new TextBlock { Text = "Nombre del analista forense" }, operatorName } });
        var caseActions = new WrapPanel();
        caseActions.Children.Add(Primary(Action("Crear caso…", CreateCase)));
        caseActions.Children.Add(Action("Abrir caso…", OpenCase));
        caseActions.Children.Add(Action("Carpeta del caso", () => { OpenPath(RequireCase().Root); return Task.CompletedTask; }));
        var welcome = new StackPanel { Spacing = 18, Children = {
            PageHeading("Tu investigación, paso a paso", "Crea un caso, añade los archivos y elige qué quieres examinar."),
            Card(new StackPanel { Spacing = 12, Children = { new TextBlock { Text = "1. Prepara el caso", FontSize = 20, FontWeight = FontWeight.SemiBold }, createBar, caseActions } }),
            Card(new StackPanel { Spacing = 10, Children = { new TextBlock { Text = "2. Añade y analiza tus evidencias", FontSize = 20, FontWeight = FontWeight.SemiBold },
                new TextBlock { Text = "Volcado Windows → Memoria RAM\nExtracción Android o iOS, ejecutable, ZIP o imagen de disco → Archivos y móviles", TextWrapping = TextWrapping.Wrap },
                Action("Ir a evidencias →", () => { navigation.SelectedIndex = 1; return Task.CompletedTask; }) } }),
            Card(new StackPanel { Spacing = 10, Children = { new TextBlock { Text = "3. Revisa los resultados y prepara el informe", FontSize = 20, FontWeight = FontWeight.SemiBold },
                new TextBlock { Text = "Busca en las tablas, abre el detalle de cada fila y añade los hallazgos a tus notas con su fuente.", TextWrapping = TextWrapping.Wrap }, caseOverview, caseLocation } })
        }};
        var evidenceActions = new WrapPanel();
        evidenceActions.Children.Add(Primary(Action("Añadir archivos…", Import, true)));
        evidenceActions.Children.Add(Action("Verificar copias", Verify));
        var evidenceTab = new StackPanel { Spacing = 16, Children = { PageHeading("Evidencias del caso", "Añade los archivos originales. KRONOS crea y verifica sus copias dentro del caso."), evidenceActions, search, evidenceList,
            new Expander { Header = "Cómo se verifica la integridad", Content = new TextBlock { Text = "Se compara SHA-256 del origen antes y después y de la copia. En archivos grandes, estas lecturas pueden tardar. No hace falta copiar nada manualmente a evidence/.", TextWrapping = TextWrapping.Wrap } } } };
        var reportTab = new StackPanel { Spacing = 12, Children = {
            PageHeading("Informe y conclusiones", "Revisa tus hallazgos, conserva sus fuentes y completa tu interpretación antes de exportar."), notes,
            Action("Guardar notas", SaveNotes), Action("Generar y abrir informe HTML + Markdown", Export)
        }};
        var modules = new StackPanel { Spacing = 12, Children = {
            new TextBlock { Text = "Módulos de artefactos Windows", FontSize = 19 },
            new TextBlock { Text = "Selecciona una carpeta de artefactos KRONOS con MANIFEST_HASHES_SHA256.csv. La correlación Python verifica primero el caso y genera observaciones, coincidencias, cobertura e informe en una carpeta nueva fuera de la evidencia.", TextWrapping = TextWrapping.Wrap },
            python, Action("Verificar carpeta de artefactos (Windows)", VerifyLegacy),
            Action("Analizar carpeta de artefactos", AnalyzeLegacy),
            new TextBlock { Text = "Adquisición Windows", FontSize = 19 },
            new TextBlock { Text = "Requiere Windows y ejecutar KRONOS como administrador. Recoge artefactos del equipo actual con el perfil Triage y los guarda dentro del caso. No incluye captura RAM ni elevación automática.", TextWrapping = TextWrapping.Wrap },
            consent, Action("Capturar datos del equipo actual…", Capture),
            new TextBlock { Text = "Para examinar archivos ya adquiridos usa Memoria RAM o Archivos y móviles. Esta sección contiene operaciones avanzadas sobre artefactos y el equipo actual.", TextWrapping = TextWrapping.Wrap }
        }};
        navigation = new TabControl { TabStripPlacement = Dock.Left, ItemsSource = new[] {
            Page("Inicio", new ScrollViewer { Content = welcome }),
            Page("1  Evidencias", new ScrollViewer { Content = evidenceTab }),
            Page("2  Memoria RAM", CreateMemoryPanel()),
            Page("2  Archivos y móviles", CreateForensicPanel()),
            Page("3  Informe", new ScrollViewer { Content = reportTab }),
            Page("Avanzado", new ScrollViewer { Content = modules }),
            Page("Actividad", results)
        }};
        var caseHeader = Card(new StackPanel { Spacing = 4, Children = { title, summary } }, 12);
        var grid = new Grid { Margin = new Thickness(20), RowDefinitions = new RowDefinitions("Auto,Auto,*,Auto,Auto"), RowSpacing = 12 };
        Add(grid, header, 0); Add(grid, caseHeader, 1); Add(grid, navigation, 2);
        Add(grid, progress, 3); Add(grid, status, 4); Content = grid;
        search.TextChanged += (_, _) => RefreshEvidence();
        evidenceList.DoubleTapped += async (_, _) =>
        {
            if (evidenceList.SelectedItem is not MemoryEvidenceChoice choice) return;
            var e = choice.Evidence;
            var detail = new Window { Title = "Evidencia · " + Path.GetFileName(e.Source), Width = 900, Height = 540,
                WindowStartupLocation = WindowStartupLocation.CenterOwner, Content = new TextBox { Margin = new Thickness(20), IsReadOnly = true,
                    AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, Text = $"Archivo: {Path.GetFileName(e.Source)}\n\nTamaño: {e.Size:N0} bytes\n\nID: {e.Id}\n\nOrigen: {e.Source}\n\nCopia del caso: {e.RelativePath}\n\nSHA-256: {e.Sha256}\n\nImportación UTC: {e.ImportedUtc:O}\n\nMétodo: {e.Method}" } };
            await detail.ShowDialog(this);
        };
        Closing += async (_, e) =>
        {
            if (busy) { e.Cancel = true; return; }
            if (current is null || current.Document.Notes == (notes.Text ?? "")) return;
            e.Cancel = true;
            busy = true;
            try { await SaveCurrentNotes(); busy = false; Close(); }
            catch (Exception ex) { busy = false; status.Text = "No se guardaron las notas: " + ex.Message; }
        };
        Closed += (_, _) => current?.Dispose();
        notes.TextChanged += (_, _) =>
        {
            if (current is not null && current.Document.Notes != (notes.Text ?? ""))
                status.Text = "Notas sin guardar. Pulsa Guardar notas antes de cerrar.";
        };
        if (initialCase is not null) { current = initialCase; LoadCase(); }
    }
    private static void Add(Grid grid, Control control, int row) { Grid.SetRow(control, row); grid.Children.Add(control); }
    private Button Action(string label, Func<Task> operation, bool cancellable = false)
    {
        var button = new Button { Content = label, Margin = new Thickness(0, 3, 10, 3), Padding = new Thickness(14, 9) };
        buttons.Add(button);
        button.Click += async (_, _) =>
        {
            if (busy) return;
            busy = true; progress.IsVisible = true;
            completionMessage = null;
            operationCancellation = new CancellationTokenSource();
            memoryCancel.IsEnabled = cancellable;
            forensicCancel.IsEnabled = cancellable;
            foreach (var b in buttons) b.IsEnabled = false;
            try { status.Text = "En curso…"; await operation(); status.Text = completionMessage ?? "Operación completada."; RefreshEvidence(); }
            catch (OperationCanceledException) { status.Text = "Operación cancelada. Consulta los registros conservados."; RefreshEvidence(); }
            catch (Exception ex) { status.Text = "Error: " + ex.Message; results.Text = $"{DateTimeOffset.UtcNow:O} · {label}\n{ex.Message}\n\n{results.Text}"; }
            finally { busy = false; progress.IsVisible = false; memoryCancel.IsEnabled = false; forensicCancel.IsEnabled = false; operationCancellation.Dispose(); operationCancellation = null; foreach (var b in buttons) b.IsEnabled = true; }
        };
        return button;
    }
    private CaseService RequireCase() => current ?? throw new InvalidOperationException("Crea o abre un caso primero.");
    private async Task<string?> Folder(string title)
    {
        var selected = await StorageProvider.OpenFolderPickerAsync(new() { Title = title, AllowMultiple = false });
        return selected.FirstOrDefault()?.TryGetLocalPath();
    }
    private async Task CreateCase()
    {
        await SaveCurrentNotes();
        var parent = await Folder("Carpeta donde crear el nuevo caso");
        if (parent is null) return;
        var created = await CaseService.CreateAsync(parent, caseId.Text ?? "", operatorName.Text ?? "");
        current?.Dispose(); current = created; LoadCase(); navigation.SelectedIndex = 1;
    }
    private async Task OpenCase()
    {
        await SaveCurrentNotes();
        var files = await StorageProvider.OpenFilePickerAsync(new() { Title = "Abrir caso KRONOS V2", AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("Caso KRONOS") { Patterns = ["*.kronos.json"] }] });
        var file = files.FirstOrDefault()?.TryGetLocalPath();
        if (file is null) return;
        var opened = await CaseService.OpenAsync(file);
        current?.Dispose(); current = opened; LoadCase();
    }
    private void LoadCase()
    {
        var c = RequireCase(); title.Text = $"{c.Document.DisplayName} · {c.Document.Operator}";
        notes.Text = c.Document.Notes; caseId.Text = c.Document.DisplayName; operatorName.Text = c.Document.Operator;
        results.Text = string.Join("\n", c.Document.Operations.Select(o => $"{o.Utc:O} | {o.Action} | {o.Detail}"));
        RefreshEvidence();
    }
    private void RefreshEvidence()
    {
        if (current is null) return;
        var filter = search.Text ?? "";
        evidenceList.ItemsSource = current.Document.Evidence.Where(e => (e.Source + e.Id + e.Sha256).Contains(filter, StringComparison.OrdinalIgnoreCase)).Select(e => new MemoryEvidenceChoice(e)).ToList();
        summary.Text = $"{current.Document.Evidence.Count} evidencias · {current.Document.MemoryAnalyses.Count + current.Document.ForensicAnalyses.Count} análisis guardados · trabajo dentro del caso";
        caseLocation.Text = "Carpeta de trabajo: " + current.Root;
        caseOverview.Text = summary.Text;
        RefreshMemory();
        RefreshForensics();
    }
    private async Task Import()
    {
        var c = RequireCase();
        var selected = await StorageProvider.OpenFilePickerAsync(new() { Title = "Importar evidencias existentes", AllowMultiple = true });
        var failures = new List<string>();
        foreach (var file in selected)
        {
            var path = file.TryGetLocalPath(); if (path is null) continue;
            try { await c.ImportAsync(path, new Progress<string>(s => status.Text = $"{file.Name}: {s}"), operationCancellation?.Token ?? CancellationToken.None); }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { failures.Add($"{file.Name}: {ex.Message}"); }
        }
        results.Text = failures.Count == 0 ? "Importación finalizada; hashes de origen y copia coincidentes." : string.Join("\n", failures);
        if (failures.Count > 0) throw new IOException($"{failures.Count} archivo(s) no importados. Consulte Resultados y actividad.");
    }
    private async Task Verify()
    {
        var c = RequireCase();
        var checks = await c.VerifyAsync();
        results.Text = checks.Count == 0 ? "El caso no contiene evidencias." : string.Join("\n", checks.Select(v => $"{v.EvidenceId}: {v.Status} — {v.Detail}"));
        if (checks.Any(c => c.Status != "COINCIDE")) throw new IOException("Verificación con incidencias. Consulte Resultados y actividad.");
    }
    private async Task SaveCurrentNotes()
    {
        if (current is not null && current.Document.Notes != (notes.Text ?? ""))
        {
            var before = current.Document.Notes;
            current.Document.Notes = notes.Text ?? "";
            try { await current.RecordAsync("Editar notas", "Notas del profesional actualizadas."); }
            catch { current.Document.Notes = before; throw; }
        }
    }
    private async Task SaveNotes() { RequireCase(); await SaveCurrentNotes(); }
    private async Task Export() { await SaveNotes(); OpenPath(await RequireCase().ExportReportAsync()); }
    private static void OpenPath(string path) => Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    private async Task Legacy(string executable, string[] args, string action)
    {
        var c = RequireCase();
        try
        {
            var workspace = c.ResolveAnalysisPath("analysis/legacy_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(workspace);
            var output = await LegacyRunner.RunAsync(executable, args, workspace);
            results.Text = output;
            await c.RecordAsync(action, string.Join(" | ", args) + "\n" + output);
        }
        catch (Exception ex) { await c.RecordAsync(action + " — ERROR", ex.Message); throw; }
    }
    private async Task VerifyLegacy()
    {
        RequireCase();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("El verificador de artefactos solo se habilita en Windows. El análisis Python también verifica el manifiesto.");
        var folder = await Folder("Carpeta de artefactos que contiene el manifiesto"); if (folder is null) return;
        await Legacy("powershell.exe", ["-NoProfile", "-NonInteractive", "-File", Path.Combine(LegacyRunner.VendorRoot, "Kronos.Verify.ps1"), "-CaseFolder", folder], "Verificar artefactos");
    }
    private async Task AnalyzeLegacy()
    {
        var c = RequireCase(); var folder = await Folder("Carpeta de artefactos para analizar"); if (folder is null) return;
        var output = c.ResolveAnalysisPath("analysis/artifacts_" + Guid.NewGuid().ToString("N"));
        await Legacy(python.Text ?? "python3", [Path.Combine(LegacyRunner.VendorRoot, "Kronos.Correlate.py"), "--case-folder", folder, "--output-dir", output], "Analizar artefactos");
        await c.RecordAsync("Resultados de artefactos", output); OpenPath(Path.Combine(output, "INFORME_INVESTIGACION.html"));
    }
    private async Task Capture()
    {
        var c = RequireCase();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Adquisición disponible solamente en Windows.");
        if (consent.IsChecked != true) throw new InvalidOperationException("Autoriza la captura del equipo actual antes de continuar. Para examinar un volcado descargado, usa Análisis RAM.");
        var parent = c.ResolveAnalysisPath("analysis/acquisition_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(parent);
        consent.IsChecked = false;
        await Legacy("powershell.exe", ["-NoProfile", "-NonInteractive", "-File", Path.Combine(LegacyRunner.VendorRoot, "Kronos.ps1"), "-CaseId", c.Document.Id,
            "-Operator", c.Document.Operator, "-OutputRoot", parent, "-CollectionProfile", "Triage", "-MemoryMode", "Skip"], "Adquisición Windows");
        OpenPath(parent);
    }
}
