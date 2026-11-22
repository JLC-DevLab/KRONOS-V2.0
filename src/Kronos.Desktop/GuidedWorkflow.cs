// Copyright 2026 JLC-DevLab. Volatility Software License 1.0.
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Kronos.Core;
namespace Kronos.Desktop;
public sealed partial class MainWindow
{
    private readonly TextBlock preparationHint=new(){Text="Selecciona una copia o respaldo. KRONOS comprobará su integridad y propondrá el análisis.",TextWrapping=Avalonia.Media.TextWrapping.Wrap};
    private Evidence? prepared;
    private Control GuidedPreparation()
    {
        var actions=new WrapPanel();
        actions.Children.Add(Primary(Action("Seleccionar copia o respaldo…",PrepareBackup,true)));
        actions.Children.Add(Action("Analizar a fondo",AnalyzePrepared,true));
        return Card(new StackPanel{Spacing=10,Children={new TextBlock{Text="Preparación y análisis automático",FontSize=20},actions,preparationHint}});
    }
    private async Task PrepareBackup()
    {
        var c=RequireCase();var inputRoot=Environment.GetEnvironmentVariable("KRONOS_INPUT_ROOT");
        var initial=inputRoot is not null?await StorageProvider.TryGetFolderFromPathAsync(inputRoot):null;
        var files=await StorageProvider.OpenFilePickerAsync(new(){Title="Seleccionar copia o respaldo",AllowMultiple=false,SuggestedStartLocation=initial});
        var path=files.FirstOrDefault()?.TryGetLocalPath();if(path is null)return;
        var progress=new Progress<string>(s=>{status.Text=s;preparationHint.Text=s;});
        var imported=await c.ImportAsync(path,progress,operationCancellation!.Token);
        var identification=await c.IdentifyAsync(imported.Id,progress,operationCancellation.Token);
        prepared=c.Document.Evidence.Single(e=>e.Id==imported.Id);
        preparationHint.Text=$"Copia verificada · {identification.Kind}\n{identification.Description}\nSHA-256: {prepared.Sha256}\nPulsa Analizar a fondo para continuar.";
        completionMessage="Copia preparada e identificada. Revisa la propuesta antes de analizar.";
    }
    private async Task AnalyzePrepared()
    {
        var c=RequireCase();
        var item=prepared is not null?c.Document.Evidence.FirstOrDefault(e=>e.Id==prepared.Id):null;
        item??=c.Document.Evidence.LastOrDefault(e=>e.Identification is not null);
        if(item?.Identification is not EvidenceIdentification identified)throw new InvalidOperationException("Selecciona y prepara primero una copia o respaldo.");
        if(identified.SuggestedTools.Count==0)throw new InvalidOperationException("Formato sin perfil compatible. No se fuerza un analizador incorrecto.");
        var progress=new Progress<string>(s=>{status.Text=s;preparationHint.Text=s;});
        var outcome=await c.AnalyzeSuggestedAsync(item.Id,progress,operationCancellation!.Token);
        RefreshEvidence();
        if(outcome.Files.Count>0){forensicRuns.SelectedItem=outcome.Files[^1];navigation.SelectedIndex=3;}
        else navigation.SelectedIndex=2;
        completionMessage="Análisis terminado. Revisa categorías, fotos, cobertura e incidencias.";
    }
}
