namespace CubeNova;

/// <summary>所有資料夾位置。遊戲資料放在 %APPDATA%\.cubenova</summary>
public static class Paths
{
    public static readonly string Root = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".cubenova");

    public static string Versions  => Path.Combine(Root, "versions");
    public static string Libraries => Path.Combine(Root, "libraries");
    public static string Assets    => Path.Combine(Root, "assets");
    public static string Runtime   => Path.Combine(Root, "runtime");
    public static string Instances => Path.Combine(Root, "instances");
    public static string WebView   => Path.Combine(Root, "webview");
    public static string Cache     => Path.Combine(Root, "cache");
    public static string SettingsFile => Path.Combine(Root, "settings.json");

    public static string AppDir  => AppContext.BaseDirectory;
    public static string WebDir  => Path.Combine(AppDir, "web");
    public static string BuiltinDir => Path.Combine(AppDir, "builtin");

    static Paths()
    {
        Directory.CreateDirectory(Root);
    }

    public static string VersionDir(string id) => Path.Combine(Versions, id);
    public static string VersionJson(string id) => Path.Combine(Versions, id, id + ".json");
    public static string VersionJar(string id) => Path.Combine(Versions, id, id + ".jar");
    public static string InstanceDir(string id) => Path.Combine(Instances, id);

    /// <summary>Maven 座標 → 相對路徑 (group:artifact:version[:classifier][@ext])</summary>
    public static string MavenToPath(string name)
    {
        string ext = "jar";
        int at = name.IndexOf('@');
        if (at >= 0) { ext = name[(at + 1)..]; name = name[..at]; }
        var p = name.Split(':');
        if (p.Length < 3) return name.Replace(':', '/');
        string file = p[1] + "-" + p[2] + (p.Length > 3 ? "-" + p[3] : "") + "." + ext;
        return p[0].Replace('.', '/') + "/" + p[1] + "/" + p[2] + "/" + file;
    }

    public static string SafeName(string s)
    {
        foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
        return s.Trim();
    }
}
