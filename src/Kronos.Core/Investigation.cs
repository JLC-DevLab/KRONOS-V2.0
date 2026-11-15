// Copyright 2026 JLC / UlysesX12. Volatility Software License 1.0.
namespace Kronos.Core;
public sealed record InvestigationResult(List<MemoryAnalysis> Memory,List<ForensicAnalysis> Files);
public sealed partial class CaseService
{
    public async Task<InvestigationResult> AnalyzeSuggestedAsync(string evidenceId,IProgress<string>? progress=null,CancellationToken cancellation=default)
    {
        var evidence=Document.Evidence.Single(e=>e.Id==evidenceId);
        var identification=evidence.Identification??await IdentifyAsync(evidenceId,progress,cancellation);
        if(identification.SuggestedTools.Count==0)throw new InvalidOperationException("El formato identificado no tiene un perfil compatible. No se fuerza otro motor.");
        var memory=new List<MemoryAnalysis>();var files=new List<ForensicAnalysis>();
        foreach(var tool in identification.SuggestedTools)
        {
            cancellation.ThrowIfCancellationRequested();
            if(tool=="memory")memory.Add(await AnalyzeMemoryAsync(evidenceId,VolatilityRunner.Discover(),false,progress,cancellation,true));
            else files.Add(await AnalyzeForensicAsync(evidenceId,tool,progress,cancellation));
        }
        await RecordAsync("Análisis guiado",$"{evidenceId}; {identification.Kind}; {memory.Count+files.Count} ejecuciones; revisar estados e incidencias.");
        return new(memory,files);
    }
}
