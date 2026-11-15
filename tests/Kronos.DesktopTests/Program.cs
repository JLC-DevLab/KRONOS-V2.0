// Copyright (c) 2026 José Luis Cabañas (JLC / UlysesX12).
// Distribuido bajo Volatility Software License 1.0. Véase LICENSE.md.
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.LogicalTree;
using Kronos.Desktop;
using Kronos.Core;

var fixture = Path.GetFullPath(Path.Combine(".tools", "kronos-ui-synthetic-" + Guid.NewGuid().ToString("N")));
Directory.CreateDirectory(fixture);
var source = Path.Combine(fixture, "synthetic.mem");
await File.WriteAllTextAsync(source, "SYNTHETIC MEMORY UI FIXTURE");
var sourceEntry = Path.Combine(fixture, "synthetic-entry.txt");
await File.WriteAllTextAsync(sourceEntry, "SYNTHETIC RUNNER");
var sampleCase = await CaseService.CreateAsync(fixture, "UI_SYNTHETIC", "Prueba UI");
var evidence = await sampleCase.ImportAsync(source);
var photoSource=Path.Combine(fixture,"synthetic.png");
await File.WriteAllBytesAsync(photoSource,Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAABgAAAAQCAIAAACDRijCAAAAGklEQVR4nGPQ6NlHFcQwatCoQaMGjRpEGQIAha8rH7VNDmMAAAAASUVORK5CYII="));
var photoEvidence=await sampleCase.ImportAsync(photoSource);
var photoRun=await sampleCase.AnalyzeForensicAsync(photoEvidence.Id,"media");
await sampleCase.AnalyzeMemoryAsync(evidence.Id, new UiSyntheticRunner(sourceEntry), true);
var artifactRun = await sampleCase.AnalyzeForensicAsync(evidence.Id, "strings");
if (artifactRun.Status != "CORRECTO") throw new Exception(artifactRun.Error);

var screenshots = args.Contains("--screenshots");
var builder = AppBuilder.Configure<App>();
if (screenshots) builder = builder.UseSkia();
builder.UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = !screenshots }).SetupWithoutStarting();
void Capture(Window target, string name)
{
    if (!screenshots) return;
    Directory.CreateDirectory(".tools/ui-preview");
    using var frame = target.CaptureRenderedFrame() ?? throw new Exception("No se pudo renderizar la captura.");
    frame.Save(".tools/ui-preview/" + name + ".png");
}
var window = new MainWindow();
window.Show();
Dispatcher.UIThread.RunJobs();
var all = window.GetVisualDescendants().ToArray();
if (all.OfType<TabControl>().Single().ItemCount != 7) throw new Exception("Faltan páginas.");
if (window.ActualThemeVariant != Avalonia.Styling.ThemeVariant.Light) throw new Exception("No usa el tema claro.");
Console.WriteLine("OK tema claro, inicio guiado y siete páginas disponibles");
Capture(window, "inicio");
all.OfType<TabControl>().Single().SelectedIndex = 1;
Dispatcher.UIThread.RunJobs();
all = window.GetVisualDescendants().ToArray();
var verify = all.OfType<Button>().Single(b => b.Content as string == "Verificar copias");
verify.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
Dispatcher.UIThread.RunJobs();
if (!window.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text?.Contains("Crea o abre un caso primero") == true))
    throw new Exception("No muestra error al verificar sin caso.");
if (!verify.IsEnabled) throw new Exception("No libera la interfaz tras un error.");
Console.WriteLine("OK interacción real: error sin caso y recuperación de la interfaz");
var tabs = all.OfType<TabControl>().Single();
for (var i = 0; i < 7; i++) { tabs.SelectedIndex = i; Dispatcher.UIThread.RunJobs(); }
Console.WriteLine("OK navegación por todas las pestañas");
window.Close();
var loaded = new MainWindow(sampleCase);
loaded.Show();
var loadedTabs = loaded.GetVisualDescendants().OfType<TabControl>().Single();
loadedTabs.SelectedIndex = 2;
for (var i = 0; i < 300; i++)
{
    Dispatcher.UIThread.RunJobs();
    if (loaded.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "SYNTHETIC_TEST_PROCESS")) break;
    Thread.Sleep(10);
}
if (!loaded.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "SYNTHETIC_TEST_PROCESS")) throw new Exception("No muestra filas del análisis guardado en la tabla RAM.");
if (loaded.GetVisualDescendants().OfType<DataGrid>().Single().Bounds.Height < 200) throw new Exception("La tabla RAM queda demasiado pequeña.");
Console.WriteLine("OK la tabla RAM muestra filas verificadas de un caso sintético guardado");
Capture(loaded, "memoria");
loadedTabs.SelectedIndex = 3;
for (var i = 0; i < 300; i++)
{
    Dispatcher.UIThread.RunJobs();
    if (loaded.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "SYNTHETIC MEMORY UI FIXTURE")) break;
    Thread.Sleep(10);
}
if (!loaded.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "SYNTHETIC MEMORY UI FIXTURE")) throw new Exception("El laboratorio no muestra filas del análisis forense guardado.");
Capture(loaded, "archivos");
if (loaded.GetVisualDescendants().OfType<DataGrid>().Single().Bounds.Height < 200) throw new Exception("La tabla del laboratorio queda demasiado pequeña.");
Console.WriteLine("OK laboratorio muestra tablas forenses verificadas con espacio de lectura");
Capture(loaded, "archivos");
var savedRuns=loaded.GetLogicalDescendants().OfType<ComboBox>().Distinct().Single(b=>b.ItemsSource is IEnumerable<ForensicAnalysis>);
savedRuns.SelectedItem=photoRun;
for(var i=0;i<100;i++){Dispatcher.UIThread.RunJobs();Thread.Sleep(10);}
var photosButton=loaded.GetVisualDescendants().OfType<Button>().Single(b=>b.Content as string=="Ver fotos");
photosButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
Window? gallery=null;
for(var i=0;i<300;i++)
{
    Dispatcher.UIThread.RunJobs();gallery=loaded.OwnedWindows.FirstOrDefault(w=>w.Title=="Fotos recuperadas · KRONOS");
    if(gallery?.GetVisualDescendants().OfType<Image>().Any(image=>image.Source is not null)==true)break;
    Thread.Sleep(10);
}
if(gallery is null||!gallery.GetVisualDescendants().OfType<Image>().Any(image=>image.Source is not null))throw new Exception("La galería no muestra la foto sintética dentro de KRONOS.");
Capture(gallery,"galeria");gallery.Close();Dispatcher.UIThread.RunJobs();
Console.WriteLine("OK galería muestra miniatura real de una imagen verificada sin visor externo");
loaded.Close();
Directory.Delete(fixture, true);

sealed class UiSyntheticRunner(string entry) : IMemoryRunner
{
    public string EntryPath => entry;
    public Task<string> GetVersionAsync(string directory, CancellationToken cancellation) => Task.FromResult(VolatilityRunner.RequiredVersion);
    public async Task<int> ExecuteAsync(MemoryCommand command, CancellationToken cancellation)
    {
        await File.WriteAllTextAsync(command.LogFile, "SYNTHETIC UI TEST");
        await File.WriteAllTextAsync(command.OutputFile, "[{\"PID\":4,\"ImageFileName\":\"SYNTHETIC_TEST_PROCESS\",\"__children\":[]}]");
        return 0;
    }
}
