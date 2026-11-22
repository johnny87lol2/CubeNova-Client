using Newtonsoft.Json.Linq;

namespace CubeNova;

/// <summary>
/// FPS Boost: 啟動前自動從 Modrinth 安裝效能模組到實例 (Fabric)，並提供優化的 JVM 參數。
/// 不會更動 Minecraft 的畫面設定。這個版本沒有對應的模組時會自動跳過。
/// </summary>
public static class Boost
{
    /// <summary>(Modrinth slug, 本機 jar 檔名前綴)</summary>
    static readonly (string Slug, string Prefix)[] Mods =
    {
        ("sodium", "sodium"),               // 渲染引擎 (最大的 FPS 提升)
        ("lithium", "lithium"),             // 遊戲邏輯優化
        ("ferrite-core", "ferritecore"),    // 記憶體優化
        ("entityculling", "entityculling"), // 不畫看不到的實體
        ("immediatelyfast", "immediatelyfast"), // 加速即時繪圖
        ("modernfix", "modernfix"),         // 載入與記憶體優化
    };

    /// <summary>常見的 G1GC 調校參數，減少卡頓</summary>
    public static readonly string[] JvmFlags =
    {
        "-XX:+UseG1GC", "-XX:+ParallelRefProcEnabled", "-XX:MaxGCPauseMillis=100",
        "-XX:+UnlockExperimentalVMOptions", "-XX:+DisableExplicitGC",
        "-XX:G1NewSizePercent=30", "-XX:G1MaxNewSizePercent=40", "-XX:G1HeapRegionSize=8M",
        "-XX:G1ReservePercent=20", "-XX:SurvivorRatio=32", "-XX:+PerfDisableSharedMem",
    };

    static void DeleteTracked(string mods, JObject installed)
    {
        foreach (var p in installed.Properties().ToList())
        {
            try { File.Delete(Path.Combine(mods, (string)p.Value!)); } catch { }
        }
        installed.RemoveAll();
    }

    public static async Task Apply(Instance inst, Action<string> report, CancellationToken ct)
    {
        if (inst.Loader != "fabric") return;
        string mods = Path.Combine(Paths.InstanceDir(inst.Id), "mods");
        Directory.CreateDirectory(mods);
        string markerPath = Path.Combine(mods, ".cubenova-boost.json");

        JObject m;
        try { m = File.Exists(markerPath) ? JObject.Parse(File.ReadAllText(markerPath)) : new JObject(); }
        catch { m = new JObject(); }
        var installed = m["installed"] as JObject ?? new JObject();
        var skip = m["skip"] as JArray ?? new JArray();
        m["installed"] = installed;
        m["skip"] = skip;

        // Minecraft 版本變了：舊的不相容，重來
        if ((string?)m["mc"] != inst.Mc)
        {
            DeleteTracked(mods, installed);
            skip.Clear();
            m["mc"] = inst.Mc;
        }

        if (!Store.Current.FpsBoost)
        {
            DeleteTracked(mods, installed);
            File.WriteAllText(markerPath, m.ToString());
            return;
        }

        foreach (var (slug, prefix) in Mods)
        {
            ct.ThrowIfCancellationRequested();
            if (installed[slug] is JToken f && File.Exists(Path.Combine(mods, (string)f!))) continue;
            if (skip.Any(x => (string?)x == slug)) continue;
            if (Directory.GetFiles(mods, prefix + "*.jar").Length > 0) continue; // 玩家自己已經裝了

            report(L.T($"FPS Boost: installing {slug}...", $"FPS 加速：安裝 {slug} 中..."));
            try
            {
                installed[slug] = await Modrinth.Install(slug, "mod", inst, null, ct);
            }
            catch (OperationCanceledException) { throw; }
            catch (NoCompatibleVersionException) { skip.Add(slug); }   // 這個 Minecraft 版本沒有對應版本：以後不再試
            catch (Exception)
            {
                break; // 多半是沒網路：這次先不裝了，下次啟動再試 (避免每個模組都等逾時)
            }
            File.WriteAllText(markerPath, m.ToString());
        }
        File.WriteAllText(markerPath, m.ToString());
    }
}
