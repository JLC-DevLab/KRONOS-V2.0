// Copyright (c) 2026 JLC-DevLab.
// Distribuido bajo Volatility Software License 1.0. Véase LICENSE.md.
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;
using Avalonia.Markup.Xaml.Styling;

namespace Kronos.Desktop;
internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        System.Globalization.CultureInfo.DefaultThreadCurrentCulture=System.Globalization.CultureInfo.GetCultureInfo("es-ES");
        System.Globalization.CultureInfo.DefaultThreadCurrentUICulture=System.Globalization.CultureInfo.GetCultureInfo("es-ES");
        if(args.Length==4&&args[0]=="--analyze-copy")
        {
            try { AnalyzeCopy(args[1],args[2],args[3]).GetAwaiter().GetResult(); }
            catch(Exception ex){Console.Error.WriteLine(ex);Environment.ExitCode=1;}
            return;
        }
        if(args.Length==1&&File.Exists(args[0])&&args[0].EndsWith(".kronos.json",StringComparison.OrdinalIgnoreCase)) App.InitialCase=Kronos.Core.CaseService.OpenAsync(args[0]).GetAwaiter().GetResult();
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace();
    private static async Task AnalyzeCopy(string source,string name,string analyst)
    {
        var parent=Environment.GetEnvironmentVariable("KRONOS_CASES_ROOT")??throw new InvalidOperationException("Usa el lanzador KRONOS para definir la carpeta de trabajo.");
        using var c=await Kronos.Core.CaseService.CreateAsync(parent,name,analyst);
        Console.WriteLine("CASE_FILE: "+Path.Combine(c.Root,Kronos.Core.CaseService.FileName));
        var progress=new Progress<string>(s=>Console.WriteLine($"{DateTimeOffset.UtcNow:O} {s}"));
        var evidence=await c.ImportAsync(source,progress);
        var identification=await c.IdentifyAsync(evidence.Id,progress);
        Console.WriteLine("IDENTIFICACION: "+identification.Kind+" · "+identification.Confidence);
        var outcome=await c.AnalyzeSuggestedAsync(evidence.Id,progress);
        var summary=System.Text.Json.JsonSerializer.Serialize(new{CaseFile=Path.Combine(c.Root,Kronos.Core.CaseService.FileName),InputSha256=evidence.Sha256,identification.Kind,
            Analyses=outcome.Files.Select(r=>new{r.Id,r.Tool,r.Status,r.Integrity,r.Rows,r.Categories,r.Error}),Memory=outcome.Memory.Select(r=>new{r.Status,r.Integrity,r.Error})},new System.Text.Json.JsonSerializerOptions{WriteIndented=true});
        await File.WriteAllTextAsync(Path.Combine(c.Root,"reports","automatic-summary.json"),summary);
        Console.WriteLine(summary);
        if(outcome.Files.Any(r=>r.Status is not("CORRECTO" or "REVISAR COBERTURA"))||outcome.Memory.Any(r=>r.Status is not("COMPLETO" or "PARCIAL")))Environment.ExitCode=1;
    }
}
public sealed class App : Application
{
    public static Kronos.Core.CaseService? InitialCase { get; set; }
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
        Styles.Add(new StyleInclude(new Uri("avares://Kronos.Desktop")) { Source = new Uri("avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml") });
        RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Light;
    }
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) desktop.MainWindow = new MainWindow(InitialCase);
        base.OnFrameworkInitializationCompleted();
    }
}
