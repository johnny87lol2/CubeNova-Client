using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace CubeNova;

/// <summary>Mojang version JSON 的 rules (library / argument 條件) 判斷</summary>
public static class Rules
{
    static bool ArchMatches(string a)
    {
        a = a.ToLowerInvariant();
        return RuntimeInformation.OSArchitecture switch
        {
            Architecture.X64 => a is "x64" or "x86_64" or "amd64",
            Architecture.Arm64 => a is "arm64" or "aarch64",
            Architecture.X86 => a == "x86",
            _ => false,
        };
    }

    public static bool Allowed(JToken? rules, Func<string, bool>? feature = null)
    {
        if (rules is not JArray arr || arr.Count == 0) return true;
        bool allowed = false;
        foreach (var r in arr)
            if (RuleMatches(r, feature)) allowed = (string?)r["action"] == "allow";
        return allowed;
    }

    static bool RuleMatches(JToken r, Func<string, bool>? feature)
    {
        var os = r["os"];
        if (os != null)
        {
            string? name = (string?)os["name"];
            if (name != null && name != "windows") return false;
            string? arch = (string?)os["arch"];
            if (arch != null && !ArchMatches(arch)) return false;
            string? ver = (string?)os["version"];
            if (ver != null && !Regex.IsMatch(Environment.OSVersion.Version.ToString(), ver)) return false;
        }
        if (r["features"] is JObject f)
        {
            foreach (var p in f.Properties())
            {
                bool want = (bool)p.Value;
                bool have = feature?.Invoke(p.Name) ?? false;
                if (want != have) return false;
            }
        }
        return true;
    }
}

public class LibEntry
{
    public string Key = "";
    public string RelPath = "";
    public string Url = "";
    public long Size = 0;
    public string FullPath => Path.Combine(Paths.Libraries, RelPath.Replace('/', Path.DirectorySeparatorChar));
}

/// <summary>版本 JSON 讀取、繼承合併 (Fabric 會 inheritsFrom 原版)</summary>
public static class VersionResolver
{
    public static JObject Resolve(string id)
    {
        var j = JObject.Parse(File.ReadAllText(Paths.VersionJson(id)));
        string? parentId = (string?)j["inheritsFrom"];
        if (parentId == null)
        {
            j["_jarId"] = id;
            return j;
        }

        var res = Resolve(parentId);
        foreach (var prop in j.Properties())
        {
            switch (prop.Name)
            {
                case "inheritsFrom":
                    break;
                case "libraries":
                {
                    var libs = new JArray();
                    foreach (var l in (JArray)prop.Value) libs.Add(l.DeepClone());
                    foreach (var l in res["libraries"] as JArray ?? new JArray()) libs.Add(l.DeepClone());
                    res["libraries"] = libs;
                    break;
                }
                case "arguments":
                {
                    var pa = res["arguments"] as JObject ?? new JObject();
                    var ca = (JObject)prop.Value;
                    var merged = new JObject();
                    foreach (var k in new[] { "game", "jvm" })
                    {
                        var arr = new JArray();
                        foreach (var x in pa[k] as JArray ?? new JArray()) arr.Add(x.DeepClone());
                        foreach (var x in ca[k] as JArray ?? new JArray()) arr.Add(x.DeepClone());
                        merged[k] = arr;
                    }
                    res["arguments"] = merged;
                    break;
                }
                default:
                    res[prop.Name] = prop.Value.DeepClone();
                    break;
            }
        }
        res["id"] = id;
        return res;
    }

    public static int JavaMajor(JObject ver) => (int?)ver["javaVersion"]?["majorVersion"] ?? 8;

    /// <summary>
    /// 整理出 classpath 用的函式庫，以及需要解壓的舊式 natives。
    /// 子版本(Fabric)的函式庫優先，同 key 只取第一個。
    /// </summary>
    public static List<LibEntry> CollectLibs(JObject ver, out List<(LibEntry lib, JToken? extract)> natives)
    {
        var result = new List<LibEntry>();
        natives = new List<(LibEntry, JToken?)>();
        var seen = new HashSet<string>();

        foreach (var lib in ver["libraries"] as JArray ?? new JArray())
        {
            if (!Rules.Allowed(lib["rules"])) continue;
            string name = (string?)lib["name"] ?? "";
            var parts = name.Split(':');
            string key = parts.Length >= 2 ? parts[0] + ":" + parts[1] + (parts.Length > 3 ? ":" + parts[3] : "") : name;
            // 舊式 natives (classifiers)
            string? nativeClassifier = (string?)lib["natives"]?["windows"];
            if (nativeClassifier != null)
            {
                nativeClassifier = nativeClassifier.Replace("${arch}", "64");
                var cl = lib["downloads"]?["classifiers"]?[nativeClassifier];
                if (cl != null)
                {
                    var ne = new LibEntry
                    {
                        Key = key + ":native",
                        RelPath = (string?)cl["path"] ?? Paths.MavenToPath(name + ":" + nativeClassifier),
                        Url = (string?)cl["url"] ?? "",
                        Size = (long?)cl["size"] ?? 0,
                    };
                    natives.Add((ne, lib["extract"]));
                }
            }

            if (!seen.Add(key)) continue;

            var art = lib["downloads"]?["artifact"];
            LibEntry e;
            if (art != null)
            {
                e = new LibEntry
                {
                    Key = key,
                    RelPath = (string?)art["path"] ?? Paths.MavenToPath(name),
                    Url = (string?)art["url"] ?? "",
                    Size = (long?)art["size"] ?? 0,
                };
            }
            else if (lib["downloads"] != null && nativeClassifier != null)
            {
                continue; // 只有 natives 的函式庫，不進 classpath
            }
            else
            {
                string rel = Paths.MavenToPath(name);
                string baseUrl = (string?)lib["url"] ?? "https://libraries.minecraft.net/";
                if (!baseUrl.EndsWith("/")) baseUrl += "/";
                e = new LibEntry
                {
                    Key = key,
                    RelPath = rel,
                    Url = baseUrl + rel,
                    Size = (long?)lib["size"] ?? 0,
                };
            }
            result.Add(e);
        }
        return result;
    }
}
