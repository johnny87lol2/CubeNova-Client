using System.IO.Compression;

namespace CubeNova;

/// <summary>依 Minecraft 需要的 Java 大版本，自動下載對應的 Adoptium JRE (Java 8 / 17 / 21 / 25 ...)</summary>
public static class JavaManager
{
    static string Dir(int major) => Path.Combine(Paths.Runtime, "java-" + major);

    public static string? Find(int major)
    {
        string d = Dir(major);
        if (!Directory.Exists(d)) return null;
        foreach (var f in Directory.EnumerateFiles(d, "java.exe", SearchOption.AllDirectories))
            if (f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar))
                return f;
        return null;
    }

    public static async Task<string> Ensure(int major, Action<string, double> report, CancellationToken ct)
    {
        var found = Find(major);
        if (found != null) return found;

        Directory.CreateDirectory(Paths.Runtime);
        string zip = Path.Combine(Paths.Runtime, $"java-{major}.zip");
        string url = $"https://api.adoptium.net/v3/binary/latest/{major}/ga/windows/x64/jre/hotspot/normal/eclipse";

        report(L.T($"Downloading Java {major}...", $"下載 Java {major} 中..."), 0);
        await Net.Download(url, zip, p => report(L.T($"Downloading Java {major}... {(int)(p * 100)}%", $"下載 Java {major} 中... {(int)(p * 100)}%"), p * 0.9), ct);

        report(L.T($"Extracting Java {major}...", $"解壓縮 Java {major} 中..."), 0.9);
        string tmp = Dir(major) + ".tmp";
        if (Directory.Exists(tmp)) Directory.Delete(tmp, true);
        await Task.Run(() => ZipFile.ExtractToDirectory(zip, tmp, true), ct);
        if (Directory.Exists(Dir(major))) Directory.Delete(Dir(major), true);
        Directory.Move(tmp, Dir(major));
        try { File.Delete(zip); } catch { }

        found = Find(major);
        if (found == null) throw new Exception(L.T($"Java {major} was downloaded but java.exe was not found.", $"Java {major} 已下載，但找不到 java.exe。"));
        report(L.T($"Java {major} ready", $"Java {major} 已就緒"), 1);
        return found;
    }
}
