using Newtonsoft.Json.Linq;

namespace CubeNova;

/// <summary>
/// CubeNova 內建 HUD 模組 (類似 Lunar Client)。
/// 模組 jar 放在程式旁邊的 builtin 資料夾: cubenova-client-universal.jar (所有版本合一) 或 cubenova-client-&lt;mc版本&gt;.jar
/// 啟動 Fabric 實例時自動複製到該實例的 mods 資料夾。
/// </summary>
public static class BuiltinMods
{
    const string TargetName = "cubenova-client.jar";

    const string UniversalName = "cubenova-client-universal.jar";

    /// <summary>Minecraft versions packed inside the universal jar (jar-in-jar entries).</summary>
    static List<string> UniversalVersions()
    {
        var list = new List<string>();
        string p = Path.Combine(Paths.BuiltinDir, UniversalName);
        if (!File.Exists(p)) return list;
        try
        {
            using var z = System.IO.Compression.ZipFile.OpenRead(p);
            foreach (var e in z.Entries)
            {
                string n = e.FullName.Replace('\\', '/');
                if (!n.StartsWith("META-INF/jars/cubenova-client-") || !n.EndsWith(".jar")) continue;
                list.Add(Path.GetFileNameWithoutExtension(n)["cubenova-client-".Length..]);
            }
        }
        catch { }
        return list;
    }

    public static string? JarFor(string mc)
    {
        // One universal jar for every supported version (preferred), else a per-version jar.
        if (UniversalVersions().Contains(mc)) return Path.Combine(Paths.BuiltinDir, UniversalName);
        string p = Path.Combine(Paths.BuiltinDir, $"cubenova-client-{mc}.jar");
        return File.Exists(p) ? p : null;
    }

    public static List<string> AvailableVersions()
    {
        var list = UniversalVersions();
        if (!Directory.Exists(Paths.BuiltinDir)) return list;
        foreach (var f in Directory.GetFiles(Paths.BuiltinDir, "cubenova-client-*.jar"))
        {
            string n = Path.GetFileNameWithoutExtension(f);
            if (n.EndsWith("universal")) continue;
            string v = n["cubenova-client-".Length..];
            if (!list.Contains(v)) list.Add(v);
        }
        return list;
    }

    public static string ConfigPath(Instance inst) =>
        Path.Combine(Paths.InstanceDir(inst.Id), "config", "cubenova.json");

    /// <summary>依實例設定安裝或移除內建模組</summary>
    public static async Task Apply(Instance inst, Action<string> report, CancellationToken ct)
    {
        string mods = Path.Combine(Paths.InstanceDir(inst.Id), "mods");
        string target = Path.Combine(mods, TargetName);

        if (inst.Loader != "fabric" || !inst.BuiltinMods)
        {
            try { if (File.Exists(target)) File.Delete(target); } catch { }
            return;
        }
        string? jar = JarFor(inst.Mc);
        if (jar == null) return; // 這個版本沒有內建模組

        Directory.CreateDirectory(mods);
        Capes.SyncTo(inst);
        var src = new FileInfo(jar);
        if (!File.Exists(target) || new FileInfo(target).Length != src.Length ||
            File.GetLastWriteTimeUtc(target) != src.LastWriteTimeUtc)
        {
            File.Copy(jar, target, true);
            File.SetLastWriteTimeUtc(target, src.LastWriteTimeUtc);
        }
        report(L.T("Installing Fabric API...", "安裝 Fabric API 中..."));
        try { await Modrinth.EnsureFabricApi(inst, ct); }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            report(L.T("Fabric API install failed: ", "Fabric API 安裝失敗：") + ex.Message);
        }
    }

    public static JObject ReadConfig(Instance inst)
    {
        try { return JObject.Parse(File.ReadAllText(ConfigPath(inst))); }
        catch { return new JObject(); }
    }

    public static void WriteConfig(Instance inst, JObject cfg)
    {
        string p = ConfigPath(inst);
        Directory.CreateDirectory(Path.GetDirectoryName(p)!);
        File.WriteAllText(p, cfg.ToString());
    }
}
