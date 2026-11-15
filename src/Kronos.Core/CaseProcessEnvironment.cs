// Copyright 2026 JLC / UlysesX12. Volatility Software License 1.0.
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Kronos.Core;

/// <summary>Private environment for each child process; never changes host/user settings.</summary>
public static class CaseProcessEnvironment
{
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetShortPathNameW")]
    private static extern uint GetShortPathName(string path, StringBuilder output, uint size);
    public static string ToolPath(string path)
    {
        if (!OperatingSystem.IsWindows()) return path;
        var buffer = new StringBuilder(32768);
        var length = GetShortPathName(Path.GetFullPath(path), buffer, (uint)buffer.Capacity);
        return length > 0 && length < buffer.Capacity ? buffer.ToString() : path;
    }
    public static void Configure(ProcessStartInfo process, string workspace)
    {
        workspace = Path.GetFullPath(workspace);
        var caseRoot = new DirectoryInfo(workspace);
        while (caseRoot is not null && !File.Exists(Path.Combine(caseRoot.FullName, CaseService.FileName))) caseRoot = caseRoot.Parent;
        if (caseRoot is null) throw new InvalidOperationException("El proceso debe trabajar dentro de un caso KRONOS.");
        // Keep PyInstaller's extraction path short, but physically inside the case.
        var privateRoot = Path.Combine(caseRoot.FullName, ".work", Path.GetFileName(workspace)[^Math.Min(8, Path.GetFileName(workspace).Length)..]);
        var temporary = Path.Combine(privateRoot, "t");
        var cache = Path.Combine(privateRoot, "c");
        var home = Path.Combine(privateRoot, "h");
        foreach (var directory in new[] { temporary, cache, home })
        {
            for (var item = new DirectoryInfo(directory); item is not null && item.FullName.Length >= caseRoot.FullName.Length; item = item.Parent)
                if (item.Exists && (item.Attributes & FileAttributes.ReparsePoint) != 0) throw new IOException("No se admiten enlaces en la carpeta de trabajo del caso.");
            Directory.CreateDirectory(directory);
        }
        process.WorkingDirectory = workspace;
        foreach (var key in new[] { "TEMP", "TMP", "TMPDIR" }) process.Environment[key] = ToolPath(temporary);
        process.Environment["XDG_CACHE_HOME"] = cache;
        process.Environment["XDG_CONFIG_HOME"] = Path.Combine(home, "config");
        process.Environment["XDG_DATA_HOME"] = Path.Combine(home, "data");
        process.Environment["LOCALAPPDATA"] = Path.Combine(home, "local");
        process.Environment["APPDATA"] = Path.Combine(home, "roaming");
        process.Environment["HOME"] = home;
        process.Environment["USERPROFILE"] = home;
        process.Environment["PYTHONDONTWRITEBYTECODE"] = "1";
        process.Environment["PYTHONPYCACHEPREFIX"] = Path.Combine(cache, "python");
        process.Environment["DOTNET_CLI_HOME"] = Path.Combine(home, "dotnet");
        process.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
    }
}
