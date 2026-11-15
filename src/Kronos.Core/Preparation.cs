// Copyright 2026 JLC / UlysesX12. Volatility Software License 1.0.
using System.Text.Json;
namespace Kronos.Core;
public sealed record EvidenceIdentification(string Kind,string Description,string Confidence,List<string> SuggestedTools);
public sealed partial class CaseService
{
    public async Task<EvidenceIdentification> IdentifyAsync(string evidenceId,IProgress<string>? progress=null,CancellationToken cancellation=default)
    {
        var evidence=Document.Evidence.Single(e=>e.Id==evidenceId);
        var input=ResolveEvidence(evidence.RelativePath);
        progress?.Report("Comprobando la copia de trabajo antes de identificarla…");
        if(await HashAsync(input,cancellation)!=evidence.Sha256)throw new InvalidDataException("La copia no coincide con el SHA-256 registrado.");
        var directory=ResolveAnalysisPath("analysis/identify_"+evidenceId[..12]);Directory.CreateDirectory(directory);
        var script=ToolRoots().Select(r=>Path.Combine(r,"engines","forensics","identify.py")).First(File.Exists);
        var output=Path.Combine(directory,"identification.json");
        var exit=await RunForensicProcess(VolatilityRunner.Discover().PythonExecutable,["-I","-B",script,input,output],directory,"identify",cancellation);
        if(exit!=0)throw new IOException(VolatilityRunner.ReadTail(Path.Combine(directory,"identify.stderr.log")));
        var result=JsonSerializer.Deserialize<EvidenceIdentification>(await File.ReadAllTextAsync(output,cancellation),Json)!;
        Document.Evidence[Document.Evidence.IndexOf(evidence)]=evidence with{Identification=result};
        await RecordAsync("Identificar copia",$"{evidenceId}; {result.Kind}; {result.Confidence}; {result.Description}");return result;
    }
}
