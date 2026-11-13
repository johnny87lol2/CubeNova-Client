using Newtonsoft.Json.Linq;

namespace CubeNova;

public static class Modrinth
{
    const string Api = "https://api.modrinth.com/v2";

    public static string FolderFor(string type) => type switch
    {
        "resourcepack" => "resourcepacks",
        "shader" => "shaderpacks",
        _ => "mods",
    };

    public static async Task<JToken> Search(string type, string query, string? mc, string? loader, int offset, string sort, CancellationToken ct)
    {
        var facets = new List<string> { $"[\"project_type:{type}\"]" };
        if (!string.IsNullOrEmpty(mc)) facets.Add($"[\"versions:{mc}\"]");
        if (type == "mod" && !string.IsNullOrEmpty(loader) && loader != "vanilla") facets.Add($"[\"categories:{loader}\"]");
        string url = $"{Api}/search?query={Uri.EscapeDataString(query)}&limit=20&offset={offset}&index={sort}" +
                     $"&facets={Uri.EscapeDataString("[" + string.Join(",", facets) + "]")}";
        return await Net.GetJson(url, ct);
    }

    /// <summary>安裝專案最新相容版本到實例，回傳檔名</summary>
    public static async Task<string> Install(string projectId, string type, Instance inst, Action<double>? progress,
        CancellationToken ct, int depth = 0)
    {
        string url = $"{Api}/project/{projectId}/version?game_versions={Uri.EscapeDataString($"[\"{inst.Mc}\"]")}";
        if (type == "mod") url += $"&loaders={Uri.EscapeDataString($"[\"{inst.Loader}\"]")}";
        var versions = await Net.GetJson(url, ct) as JArray ?? new JArray();
        if (versions.Count == 0)
            throw new Exception(L.T($"No version compatible with Minecraft {inst.Mc} ({inst.Loader}).", $"沒有相容 Minecraft {inst.Mc} ({inst.Loader}) 的版本。"));

        var v = versions.FirstOrDefault(x => (string?)x["version_type"] == "release") ?? versions[0];
        var files = v["files"] as JArray ?? new JArray();
        var file = files.FirstOrDefault(f => (bool?)f["primary"] == true) ?? files.FirstOrDefault()
            ?? throw new Exception(L.T("Version has no files.", "此版本沒有檔案。"));

        string folder = Path.Combine(Paths.InstanceDir(inst.Id), FolderFor(type));
        Directory.CreateDirectory(folder);
        string name = Paths.SafeName((string)file["filename"]!);
        string dest = Path.Combine(folder, name);
        await Net.Download((string)file["url"]!, dest, progress, ct);

        // 必要的相依模組
        if (type == "mod" && depth < 2)
        {
            foreach (var d in v["dependencies"] as JArray ?? new JArray())
            {
                if ((string?)d["dependency_type"] != "required") continue;
                string? dep = (string?)d["project_id"];
                if (dep == null || dep == "P7dR8mSH") continue; // Fabric API 另外處理
                try { await Install(dep, "mod", inst, null, ct, depth + 1); } catch { }
            }
        }
        return name;
    }

    /// <summary>確保 Fabric API 存在 (內建模組需要)</summary>
    public static async Task EnsureFabricApi(Instance inst, CancellationToken ct)
    {
        string mods = Path.Combine(Paths.InstanceDir(inst.Id), "mods");
        Directory.CreateDirectory(mods);
        if (Directory.GetFiles(mods, "fabric-api*.jar").Length > 0) return;
        await Install("P7dR8mSH", "mod", inst, null, ct);
    }
}
