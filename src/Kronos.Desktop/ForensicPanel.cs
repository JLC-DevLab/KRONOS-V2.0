// Copyright 2026 JLC-DevLab. Volatility Software License 1.0.
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using Avalonia.Media;
using Kronos.Core;

namespace Kronos.Desktop;

public sealed partial class MainWindow
{
    private readonly ComboBox forensicEvidence = new() { PlaceholderText = "1. Selecciona un archivo del caso", HorizontalAlignment = HorizontalAlignment.Stretch };
    private readonly ComboBox forensicTools = new() { ItemsSource = ForensicTool.Catalog, SelectedIndex = -1, PlaceholderText = "2. Elige qué quieres analizar", HorizontalAlignment = HorizontalAlignment.Stretch };
    private readonly ComboBox forensicRuns = new() { PlaceholderText = "Resultados guardados", HorizontalAlignment = HorizontalAlignment.Stretch };
    private readonly TextBlock forensicScope = new() { TextWrapping = TextWrapping.Wrap, Foreground = Brush.Parse("#536579") };
    private readonly TextBlock forensicHint = new() { Text = "Importa una evidencia y elige una herramienta. Los análisis se guardan en el caso.", TextWrapping = TextWrapping.Wrap, MaxHeight = 90 };
    private readonly TextBox forensicQuery = new() { Watermark = "Buscar texto, URL, nombre de archivo, artefacto…" };
    private readonly Button forensicCancel = new() { Content = "Cancelar", IsEnabled = false };
    private readonly DataGrid forensicGrid = new() { IsReadOnly = true, AutoGenerateColumns = false, CanUserResizeColumns = true,
        GridLinesVisibility = DataGridGridLinesVisibility.Horizontal, FontSize = 15, RowHeight = 32, MinHeight = 150 };
    private MemoryTable? forensicTable;
    private int forensicGeneration;
    private Button? recoveredAction;
    private readonly ComboBox forensicCategories=new(){PlaceholderText="Ver por categoría",HorizontalAlignment=HorizontalAlignment.Stretch};
    private bool updatingCategories;

    private Control CreateForensicPanel()
    {
        forensicTools.SelectionChanged += (_, _) => forensicScope.Text = (forensicTools.SelectedItem as ForensicTool)?.Scope;
        forensicScope.Text = "Android → ALEAPP · iPhone/iPad → iLEAPP · Recuperación de archivos → Foremost. Los originales no se ejecutan.";
        forensicCancel.Click += (_, _) => operationCancellation?.Cancel();
        forensicRuns.SelectionChanged += async (_, _) => await LoadForensicResults();
        forensicCategories.SelectionChanged+=async(_,_)=>{if(!updatingCategories)await LoadForensicResults();};
        forensicQuery.TextChanged += (_, _) => forensicGrid.ItemsSource = forensicTable?.Rows.Where(row => row.Cells.Values.Any(v => v.Contains(forensicQuery.Text ?? "", StringComparison.OrdinalIgnoreCase))).ToList();
        forensicGrid.DoubleTapped += async (_, _) =>
        {
            if (forensicGrid.SelectedItem is not MemoryRow row) return;
            var dialog = new Window { Title = "Resultado forense · detalle y fuente", Width = 950, Height = 650,
                WindowStartupLocation = WindowStartupLocation.CenterOwner, Content = new TextBox { Margin = new Thickness(16),
                    IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap,
                    Text = string.Join("\n\n", row.Cells.Select(c => c.Key + ": " + c.Value)) + "\n\nJSON Pointer: " + row.JsonPointer } };
            await dialog.ShowDialog(this);
        };
        var runActions = new WrapPanel();
        runActions.Children.Add(Action("Importar archivo…", Import, true));
        runActions.Children.Add(Primary(Action("Analizar archivo", RunForensic, true)));
        runActions.Children.Add(forensicCancel);
        var tableActions = new WrapPanel();
        tableActions.Children.Add(Primary(Action("Ver fotos",OpenGallery)));
        tableActions.Children.Add(Action("Añadir fila a notas", AddForensicFinding));
        recoveredAction = Action("Importar recuperado", ImportRecovered); recoveredAction.IsVisible = false;
        tableActions.Children.Add(recoveredAction);
        runActions.Children.Add(Action("Carpeta de resultados", () =>
        {
            if (forensicRuns.SelectedItem is not ForensicAnalysis run) throw new InvalidOperationException("Selecciona un análisis guardado.");
            OpenPath(RequireCase().ResolveAnalysisPath(run.RelativeDirectory)); return Task.CompletedTask;
        }));
        var choice = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*"), ColumnSpacing = 10 };
        choice.Children.Add(forensicEvidence); Grid.SetColumn(forensicTools, 1); choice.Children.Add(forensicTools);
        var history = new Expander { Header = "Análisis guardados · cambia de resultado", Content = forensicRuns, IsExpanded = false, HorizontalAlignment = HorizontalAlignment.Stretch };
        var categoriesRow=new Grid{ColumnDefinitions=new ColumnDefinitions("*,*"),ColumnSpacing=10};
        categoriesRow.Children.Add(history);Grid.SetColumn(forensicCategories,1);categoriesRow.Children.Add(forensicCategories);
        var searchBar = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 10 };
        searchBar.Children.Add(forensicQuery); Grid.SetColumn(tableActions, 1); searchBar.Children.Add(tableActions);
        var manual=new Expander{Header="Análisis manual y perfiles avanzados (opcional)",IsExpanded=false,HorizontalAlignment=HorizontalAlignment.Stretch,Content=new StackPanel{Spacing=8,Children={choice,forensicScope,runActions}}};
        var top = new StackPanel { Spacing = 8, Children = { PageHeading("Resultados de la investigación", "Mensajes, identidad, llamadas, ubicaciones y fotos, con sus fuentes."), categoriesRow, searchBar,manual } };
        var grid = new Grid { RowDefinitions = new RowDefinitions("Auto,*,Auto"), RowSpacing = 8 };
        grid.Children.Add(top); Grid.SetRow(forensicGrid, 1); grid.Children.Add(forensicGrid);
        Grid.SetRow(forensicHint, 2); grid.Children.Add(forensicHint);
        return grid;
    }
    private void RefreshForensics()
    {
        if (current is null) return;
        var id = (forensicEvidence.SelectedItem as MemoryEvidenceChoice)?.Evidence.Id;
        var choices = current.Document.Evidence.Select(e => new MemoryEvidenceChoice(e)).ToList();
        forensicEvidence.ItemsSource = choices;
        forensicEvidence.SelectedItem = choices.FirstOrDefault(e => e.Evidence.Id == id) ?? choices.LastOrDefault();
        var selected = (forensicRuns.SelectedItem as ForensicAnalysis)?.Id;
        forensicRuns.ItemsSource = current.Document.ForensicAnalyses.ToList();
        forensicRuns.SelectedItem = current.Document.ForensicAnalyses.FirstOrDefault(r => r.Id == selected) ?? current.Document.ForensicAnalyses.LastOrDefault();
    }
    private async Task RunForensic()
    {
        var c = RequireCase();
        if (forensicEvidence.SelectedItem is not MemoryEvidenceChoice choice || forensicTools.SelectedItem is not ForensicTool tool)
            throw new InvalidOperationException("Selecciona evidencia y herramienta.");
        var run = await c.AnalyzeForensicAsync(choice.Evidence.Id, tool.Id, new Progress<string>(m => { status.Text = m; forensicHint.Text = m; }), operationCancellation!.Token);
        forensicRuns.ItemsSource = c.Document.ForensicAnalyses.ToList(); forensicRuns.SelectedItem = run;
        results.Text = $"{tool.Name}: {run.Status}; integridad {run.Integrity}; {run.Rows:N0} filas\n{run.Coverage}\n{run.Error}";
        completionMessage = $"{tool.Name}: {run.Status}. Consulta las filas y la cobertura del análisis.";
    }
    private async Task LoadForensicResults()
    {
        var generation = ++forensicGeneration;
        forensicTable = null; forensicGrid.ItemsSource = null; forensicGrid.Columns.Clear();
        if (current is null || forensicRuns.SelectedItem is not ForensicAnalysis run) return;
        var options=new[]{"Todos los resultados"}.Concat(run.Categories.Where(p=>p.Key!="Cobertura").OrderBy(p=>p.Key).Select(p=>$"{p.Key} ({p.Value:N0})")).Append("Cobertura").ToList();
        var previous=forensicCategories.SelectedItem as string;
        updatingCategories=true;
        forensicCategories.ItemsSource=options;forensicCategories.SelectedItem=options.Contains(previous??"")?previous:options.FirstOrDefault(s=>s.StartsWith("Identidad y cuentas (",StringComparison.Ordinal))??options[0];
        updatingCategories=false;
        forensicTools.SelectedItem=ForensicTool.Catalog.FirstOrDefault(t=>t.Id==run.Tool);
        if (recoveredAction is not null) recoveredAction.IsVisible = run.Tool == "foremost";
        forensicHint.Text = $"{run.Tool} {run.Version} · {run.Status} · integridad {run.Integrity}\n{run.Coverage}\n{run.Error}";
        if (run.Status is not ("CORRECTO" or "REVISAR COBERTURA")) return;
        try
        {
            var selection=forensicCategories.SelectedItem as string;
            var category=selection is null||selection=="Todos los resultados"?null:selection.Split(" (")[0];
            var table = await current.LoadForensicTableAsync(run,category);
            if (generation != forensicGeneration) return;
            forensicTable = table;
            var visibleColumns=new[]{"Resumen","Categoria","Nombre","Titulo","Texto","URL","UTC"}.Where(table.Columns.Contains).Concat(table.Columns).Distinct();
            if(table.Columns.Contains("Resumen"))visibleColumns=visibleColumns.Where(key=>key is not("Datos" or "Artefacto" or "Fuente"));
            foreach (var column in visibleColumns)
            {
                var key = column;
                forensicGrid.Columns.Add(new DataGridTextColumn { Header = key, MaxWidth = 650,
                    Binding = new Binding { Converter = new FuncValueConverter<MemoryRow, string>(row => row?.Cells.GetValueOrDefault(key) ?? "") } });
            }
            forensicGrid.ItemsSource = table.Rows.Where(row => row.Cells.Values.Any(v => v.Contains(forensicQuery.Text ?? "", StringComparison.OrdinalIgnoreCase))).ToList();
            forensicHint.Text += $"\n{table.TotalRows:N0} filas; vista previa de hasta 10000. Salidas verificadas. Doble clic para ver el texto completo.";
        }
        catch (Exception ex) { if (generation == forensicGeneration) forensicHint.Text = "No se pueden mostrar resultados: " + ex.Message; }
    }
    private Task AddForensicFinding()
    {
        RequireCase();
        if (forensicRuns.SelectedItem is not ForensicAnalysis run || forensicGrid.SelectedItem is not MemoryRow row)
            throw new InvalidOperationException("Selecciona una fila del análisis.");
        notes.Text += $"\n\nHallazgo · {run.Tool} {run.Version} · evidencia {run.EvidenceId}\nFuente: {run.RelativeDirectory}/results.json#{row.JsonPointer}\n" +
            string.Join("; ", row.Cells.Select(c => c.Key + ": " + c.Value)) + "\nInterpretación del profesional: ";
        completionMessage = "Fila añadida con su fuente. Revisa la interpretación y guarda las notas.";
        return Task.CompletedTask;
    }
    private async Task ImportRecovered()
    {
        var c = RequireCase();
        if (forensicRuns.SelectedItem is not ForensicAnalysis run || forensicGrid.SelectedItem is not MemoryRow row || !row.Cells.TryGetValue("Fuente", out var file))
            throw new InvalidOperationException("Selecciona una fila de archivo recuperado por Foremost.");
        var derived = await c.ImportRecoveredAsync(run, file, operationCancellation!.Token);
        RefreshForensics();
        forensicEvidence.SelectedItem = ((IEnumerable<MemoryEvidenceChoice>)forensicEvidence.ItemsSource!).FirstOrDefault(e => e.Evidence.Id == derived.Id);
        completionMessage = "Archivo recuperado importado con vínculo a la evidencia original. Elige la siguiente herramienta.";
    }
}
