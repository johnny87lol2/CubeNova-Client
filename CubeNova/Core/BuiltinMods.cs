using Newtonsoft.Json.Linq;

namespace CubeNova;

/// <summary>
/// CubeNova 內建 HUD 模組 (類似 Lunar Client)。
/// 模組 jar 放在程式旁邊的 builtin 資料夾，檔名: cubenova-client-&lt;mc版本&gt;.jar
/// 啟動 Fabric 實例時自動複製到該實例的 mods 資料夾。
/// </summary>
public static class BuiltinMods
{
    const string TargetName = "cubenova-client.jar";

    public static string? JarFor(string mc)
    {
        string p = Path.Combine(Paths.BuiltinDir, $"cubenova-client-{mc}.jar");
        return File.Exists(p) ? p : null;
    }

    public static List<string> AvailableVersions()
    {
        var list = new List<string>();
        if (!Directory.Exists(Paths.BuiltinDir)) return list;
        foreach (var f in Directory.GetFiles(Paths.BuiltinDir, "cubenova-client-*.jar"))
        {
            string n = Path.GetFileNameWithoutExtension(f);
            list.Add(n["cubenova-client-".Length..]);
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
