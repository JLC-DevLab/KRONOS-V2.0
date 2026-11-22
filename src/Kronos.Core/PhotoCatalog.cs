// Copyright 2026 JLC-DevLab. Volatility Software License 1.0.
using System.Security.Cryptography;
using System.Text.Json;
namespace Kronos.Core;
public sealed record PhotoArtifact(string File,long Bytes,string Sha256);
public sealed partial class CaseService
{
    private readonly Dictionary<string,List<PhotoArtifact>> photoCatalogs=[];
    private readonly Dictionary<string,string> photoManifestVersions=[];
    public async Task<List<PhotoArtifact>> LoadPhotosAsync(ForensicAnalysis run)
    {
        if(!Document.ForensicAnalyses.Contains(run)||run.Integrity!="COINCIDE")throw new InvalidDataException("Integridad no verificada.");
        var manifest=ResolveAnalysisPath(run.RelativeDirectory+"/outputs-manifest.json");
        if(await HashAsync(manifest)!=run.OutputsManifestSha256)throw new InvalidDataException("El manifiesto de resultados ha cambiado.");
        if(photoManifestVersions.GetValueOrDefault(run.Id)==run.OutputsManifestSha256&&photoCatalogs.TryGetValue(run.Id,out var cached))return cached;
        var files=JsonSerializer.Deserialize<List<MemoryArtifact>>(await File.ReadAllTextAsync(manifest),Json)??[];
        var extensions=new[]{".jpg",".jpeg",".png",".gif",".bmp",".webp"};
        var eligible=files.Where(f=>f.File.StartsWith("recovered/",StringComparison.Ordinal)||f.File.Contains("/media/",StringComparison.Ordinal)||f.File.Contains("/data/",StringComparison.Ordinal)).ToList();
        var recognized=new HashSet<string>(eligible.Where(f=>extensions.Contains(Path.GetExtension(f.File).ToLowerInvariant())).Select(f=>f.File));
        using var slots=new SemaphoreSlim(8);
        var signatures=await Task.WhenAll(eligible.Where(f=>!recognized.Contains(f.File)&&f.Bytes is >=16 and <=67108864).Take(10000).Select(async file=>
        {
            await slots.WaitAsync();
            try
            {
                var path=ResolveAnalysisPath(run.RelativeDirectory+"/"+file.File);
                await using var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read,4096,true);
                var header=new byte[16];await stream.ReadExactlyAsync(header);
                return header[0]==255&&header[1]==216&&header[2]==255||header.AsSpan(0,4).SequenceEqual(new byte[]{137,80,78,71})||header.AsSpan(0,3).SequenceEqual("GIF"u8)||header.AsSpan(0,2).SequenceEqual("BM"u8)||header.AsSpan(0,4).SequenceEqual("RIFF"u8)&&header.AsSpan(8,4).SequenceEqual("WEBP"u8)?file.File:null;
            }
            catch(IOException){return null;}
            catch(InvalidDataException){return null;}
            finally{slots.Release();}
        }));
        foreach(var file in signatures.OfType<string>())recognized.Add(file);
        var photos=eligible.Where(f=>recognized.Contains(f.File))
            .OrderByDescending(f=>f.File.Contains("/media/",StringComparison.Ordinal)).DistinctBy(f=>f.Sha256).Select(f=>new PhotoArtifact(f.File,f.Bytes,f.Sha256)).ToList();
        photoCatalogs[run.Id]=photos;photoManifestVersions[run.Id]=run.OutputsManifestSha256;return photos;
    }
    public async Task<byte[]> LoadPhotoBytesAsync(ForensicAnalysis run,PhotoArtifact photo)
    {
        if(!photoCatalogs.TryGetValue(run.Id,out var catalog)||!catalog.Contains(photo))throw new InvalidDataException("Imagen no incluida en el catálogo verificado.");
        if(photo.Bytes>64*1024*1024)throw new InvalidDataException("Imagen superior a 64 MiB; conservada sin vista previa.");
        var bytes=await File.ReadAllBytesAsync(ResolveAnalysisPath(run.RelativeDirectory+"/"+photo.File));
        if(bytes.Length!=photo.Bytes||Convert.ToHexString(SHA256.HashData(bytes))!=photo.Sha256)throw new InvalidDataException("La imagen ha cambiado respecto al manifiesto.");
        return bytes;
    }
}
