using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace CubeNova;

/// <summary>下載版本、函式庫、資源、Java，組出參數並啟動遊戲</summary>
public static class GameLauncher
{
    const string ManifestUrl = "https://piston-meta.mojang.com/mc/game/version_manifest_v2.json";
    static JObject? _manifest;

    public static async Task<JObject> GetManifest(CancellationToken ct = default)
    {
        if (_manifest != null) return _manifest;
        string cache = Path.Combine(Paths.Cache, "version_manifest.json");
        try
        {
            string s = await Net.GetString(ManifestUrl, ct);
            Directory.CreateDirectory(Paths.Cache);
            File.WriteAllText(cache, s);
            _manifest = JObject.Parse(s);
        }
        catch when (File.Exists(cache))
        {
            _manifest = JObject.Parse(File.ReadAllText(cache));
        }
        return _manifest!;
    }

    public static async Task EnsureVanillaJson(string mc, CancellationToken ct)
    {
        string path = Paths.VersionJson(mc);
        if (File.Exists(path)) return;
        var man = await GetManifest(ct);
        var v = (man["versions"] as JArray)?.FirstOrDefault(x => (string?)x["id"] == mc);
        if (v == null) throw new Exception(L.T($"Minecraft version {mc} not found.", $"找不到 Minecraft 版本 {mc}。"));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await Net.Download((string)v["url"]!, path, null, ct);
    }

    /// <summary>取得 Fabric 最新穩定 loader 版本</summary>
    public static async Task<string> LatestFabricLoader(string mc, CancellationToken ct)
    {
        var arr = await Net.GetJson($"https://meta.fabricmc.net/v2/versions/loader/{Uri.EscapeDataString(mc)}", ct) as JArray;
        if (arr == null || arr.Count == 0)
            throw new Exception(L.T($"Fabric is not available for Minecraft {mc}.", $"Minecraft {mc} 沒有 Fabric 可用。"));
        var stable = arr.FirstOrDefault(x => (bool?)x["loader"]?["stable"] == true) ?? arr[0];
        return (string)stable["loader"]!["version"]!;
    }

    /// <summary>確保 Fabric profile JSON 存在，回傳要啟動的版本 id</summary>
    static async Task<string> EnsureFabric(Instance inst, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(inst.LoaderVersion))
        {
            inst.LoaderVersion = await LatestFabricLoader(inst.Mc, ct);
            Store.Save();
        }
        string id = $"fabric-loader-{inst.LoaderVersion}-{inst.Mc}";
        string path = Paths.VersionJson(id);
        if (!File.Exists(path))
        {
            string url = $"https://meta.fabricmc.net/v2/versions/loader/{Uri.EscapeDataString(inst.Mc)}/{Uri.EscapeDataString(inst.LoaderVersion)}/profile/json";
            string json = await Net.GetString(url, ct);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, json);
        }
        return id;
    }

    // ───────────────────────── 安裝 ─────────────────────────

    /// <summary>安裝全部需要的檔案，回傳 (啟動版本 id, java 路徑)</summary>
    public static async Task<(string launchId, string javaPath, JObject ver)> Prepare(
        Instance inst, Action<string, double> report, CancellationToken ct)
    {
        report(L.T("Fetching version info...", "取得版本資訊中..."), 0.01);
        await EnsureVanillaJson(inst.Mc, ct);
        string launchId = inst.Mc;
        if (inst.Loader == "fabric") launchId = await EnsureFabric(inst, ct);

        var ver = VersionResolver.Resolve(launchId);
        int javaMajor = VersionResolver.JavaMajor(ver);

        // Java
        string javaPath = await JavaManager.Ensure(javaMajor, (t, p) => report(t, 0.03 + p * 0.17), ct);

        // client jar
        string jarId = (string)ver["_jarId"]!;
        string jarPath = Paths.VersionJar(jarId);
        if (!File.Exists(jarPath))
        {
            var vanilla = jarId == launchId ? ver : VersionResolver.Resolve(jarId);
            string url = (string?)vanilla["downloads"]?["client"]?["url"]
                ?? throw new Exception(L.T("Client jar URL missing.", "找不到 client.jar 下載網址。"));
            await Net.Download(url, jarPath, p => report(L.T("Downloading Minecraft...", "下載 Minecraft 中...") + $" {(int)(p * 100)}%", 0.20 + p * 0.10), ct);
        }

        // libraries
        var libs = VersionResolver.CollectLibs(ver, out var natives);
        var items = new List<DlItem>();
        foreach (var l in libs.Concat(natives.Select(n => n.lib)))
            if (!string.IsNullOrEmpty(l.Url))
                items.Add(new DlItem { Url = l.Url, Path = l.FullPath, Size = l.Size });
        await Net.DownloadMany(items, 16,
            (d, t) => report(L.T($"Libraries {d}/{t}", $"函式庫 {d}/{t}"), 0.30 + (t == 0 ? 0 : (double)d / t) * 0.20), ct);

        // natives (舊式) 解壓
        string nativesDir = NativesDir(launchId);
        Directory.CreateDirectory(nativesDir);
        foreach (var (lib, extract) in natives)
        {
            if (!File.Exists(lib.FullPath)) continue;
            var excludes = (extract?["exclude"] as JArray)?.Select(x => (string)x!).ToList() ?? new List<string>();
            try
            {
                using var zip = ZipFile.OpenRead(lib.FullPath);
                foreach (var e in zip.Entries)
                {
                    if (string.IsNullOrEmpty(e.Name)) continue;
                    if (e.FullName.StartsWith("META-INF/")) continue;
                    if (excludes.Any(x => e.FullName.StartsWith(x))) continue;
                    try { e.ExtractToFile(Path.Combine(nativesDir, e.Name), true); } catch { }
                }
            }
            catch { }
        }

        // assets
        await EnsureAssets(ver, report, ct);
        return (launchId, javaPath, ver);
    }

    public static string NativesDir(string launchId) => Path.Combine(Paths.Versions, launchId, "natives");

    static async Task EnsureAssets(JObject ver, Action<string, double> report, CancellationToken ct)
    {
        var ai = ver["assetIndex"];
        if (ai == null) return;
        string id = (string)ai["id"]!;
        string indexPath = Path.Combine(Paths.Assets, "indexes", id + ".json");
        if (!File.Exists(indexPath))
            await Net.Download((string)ai["url"]!, indexPath, null, ct);

        var index = JObject.Parse(File.ReadAllText(indexPath));
        var objects = index["objects"] as JObject ?? new JObject();
        var items = new List<DlItem>();
        foreach (var p in objects.Properties())
        {
            string hash = (string)p.Value["hash"]!;
            string sub = hash[..2];
            items.Add(new DlItem
            {
                Url = $"https://resources.download.minecraft.net/{sub}/{hash}",
                Path = Path.Combine(Paths.Assets, "objects", sub, hash),
                Size = (long?)p.Value["size"] ?? 0,
            });
        }
        // 資源檔少數失敗只是缺音效/語言，不擋啟動
        await Net.DownloadMany(items, 24,
            (d, t) => report(L.T($"Assets {d}/{t}", $"資源 {d}/{t}"), 0.50 + (t == 0 ? 0 : (double)d / t) * 0.40), ct,
            tolerateFailures: Math.Max(5, items.Count / 50));

        // 舊版 (<=1.7) 的 virtual assets
        if ((bool?)index["virtual"] == true || (bool?)index["map_to_resources"] == true)
        {
            string virt = Path.Combine(Paths.Assets, "virtual", "legacy");
            foreach (var p in objects.Properties())
            {
                string hash = (string)p.Value["hash"]!;
                string src = Path.Combine(Paths.Assets, "objects", hash[..2], hash);
                string dst = Path.Combine(virt, p.Name.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(src) && !File.Exists(dst))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
                    File.Copy(src, dst);
                }
            }
        }
    }

    // ───────────────────────── 啟動 ─────────────────────────

    public static string OfflineUuid(string name)
    {
        // 與 Java UUID.nameUUIDFromBytes("OfflinePlayer:" + name) 相同
        byte[] hash = MD5.HashData(Encoding.UTF8.GetBytes("OfflinePlayer:" + name));
        hash[6] = (byte)((hash[6] & 0x0f) | 0x30);
        hash[8] = (byte)((hash[8] & 0x3f) | 0x80);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static Process Start(Instance inst, Account acc, string launchId, string javaPath, JObject ver,
        Action<string> onLine)
    {
        string gameDir = Paths.InstanceDir(inst.Id);
        Directory.CreateDirectory(gameDir);

        var libs = VersionResolver.CollectLibs(ver, out _);
        string jarId = (string)ver["_jarId"]!;
        var cp = libs.Select(l => l.FullPath).Where(File.Exists).ToList();
        cp.Add(Paths.VersionJar(jarId));
        string classpath = string.Join(";", cp.Distinct());

        string assetsId = (string?)ver["assetIndex"]?["id"] ?? "legacy";
        string indexPath = Path.Combine(Paths.Assets, "indexes", assetsId + ".json");
        string gameAssets = Paths.Assets;
        if (File.Exists(indexPath))
        {
            var idx = JObject.Parse(File.ReadAllText(indexPath));
            if ((bool?)idx["virtual"] == true || (bool?)idx["map_to_resources"] == true)
                gameAssets = Path.Combine(Paths.Assets, "virtual", "legacy");
        }

        bool ms = acc.Type == "microsoft";
        var vars = new Dictionary<string, string>
        {
            ["auth_player_name"] = acc.Name,
            ["version_name"] = launchId,
            ["game_directory"] = gameDir,
            ["assets_root"] = Paths.Assets,
            ["assets_index_name"] = assetsId,
            ["game_assets"] = gameAssets,
            ["auth_uuid"] = acc.Uuid,
            ["auth_access_token"] = ms ? acc.Token : "0",
            ["auth_session"] = ms ? "token:" + acc.Token + ":" + acc.Uuid : "0",
            ["clientid"] = "",
            ["auth_xuid"] = "",
            ["user_type"] = ms ? "msa" : "legacy",
            ["version_type"] = (string?)ver["type"] ?? "release",
            ["user_properties"] = "{}",
            ["natives_directory"] = NativesDir(launchId),
            ["launcher_name"] = "CubeNova",
            ["launcher_version"] = "2.0",
            ["classpath"] = classpath,
            ["library_directory"] = Paths.Libraries,
            ["classpath_separator"] = ";",
        };
        string Sub(string s) => Regex.Replace(s, @"\$\{(\w+)\}", m => vars.TryGetValue(m.Groups[1].Value, out var v) ? v : m.Value);

        List<string> Expand(JArray? arr)
        {
            var list = new List<string>();
            if (arr == null) return list;
            foreach (var a in arr)
            {
                if (a.Type == JTokenType.String) { list.Add(Sub((string)a!)); continue; }
                if (!Rules.Allowed(a["rules"])) continue; // 功能旗標(demo/自訂解析度)一律視為 false
                var val = a["value"];
                if (val is JArray va) foreach (var x in va) list.Add(Sub((string)x!));
                else if (val != null) list.Add(Sub((string)val!));
            }
            return list;
        }

        var jvm = new List<string>();
        var game = new List<string>();
        if (ver["arguments"] is JObject args)
        {
            jvm = Expand(args["jvm"] as JArray);
            game = Expand(args["game"] as JArray);
        }
        else
        {
            game = ((string?)ver["minecraftArguments"] ?? "")
                .Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(Sub).ToList();
        }
        if (!jvm.Contains("-cp") && !jvm.Contains("-classpath"))
        {
            jvm.Add("-cp");
            jvm.Add(classpath);
        }
        if (!jvm.Any(x => x.StartsWith("-Djava.library.path=")))
            jvm.Insert(0, "-Djava.library.path=" + NativesDir(launchId));

        int ram = inst.RamMb > 0 ? inst.RamMb : Store.Current.RamMb;
        var final = new List<string> { $"-Xmx{ram}M", $"-Xms{Math.Min(ram, 1024)}M" };
        final.AddRange(Store.Current.JvmArgs.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        final.AddRange(jvm);
        final.Add((string)ver["mainClass"]!);
        final.AddRange(game);

        var psi = new ProcessStartInfo(javaPath)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = gameDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        foreach (var a in final) psi.ArgumentList.Add(a);

        var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
        proc.OutputDataReceived += (_, e) => { if (e.Data != null) onLine(e.Data); };
        proc.ErrorDataReceived += (_, e) => { if (e.Data != null) onLine(e.Data); };
        proc.Start();
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();
        return proc;
    }
}
