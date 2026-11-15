namespace CubeNova;

/// <summary>自訂披風 (只有自己看得到)：存在本機，啟動遊戲時複製到實例的 config 資料夾，由模組讀取。</summary>
public static class Capes
{
    /// <summary>檢查 PNG 標頭與披風尺寸 (64x32 的整數倍，最大 1024x512)；回傳錯誤訊息，沒問題回 null</summary>
    public static string? Validate(byte[] png)
    {
        if (png.Length > 4 * 1024 * 1024) return L.T("File too large (max 4 MB).", "檔案太大 (上限 4 MB)。");
        byte[] sig = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        if (png.Length < 33 || !png.Take(8).SequenceEqual(sig)) return L.T("Not a PNG file.", "這不是 PNG 檔。");
        int w = (png[16] << 24) | (png[17] << 16) | (png[18] << 8) | png[19];
        int h = (png[20] << 24) | (png[21] << 16) | (png[22] << 8) | png[23];
        bool ok = w >= 64 && w <= 1024 && w % 64 == 0 && h * 2 == w;
        return ok ? null : L.T($"Cape size must be 64x32 or a multiple of it (yours is {w}x{h}).",
                               $"披風尺寸必須是 64x32 或其整數倍 (你的是 {w}x{h})。");
    }

    static string FileOf(string kind) => Path.Combine(Paths.Root, kind == "elytra" ? "elytra.png" : "cape.png");
    static string Norm(string kind) => kind == "elytra" ? "elytra" : "cape";

    public static bool HasKind(string kind) => File.Exists(FileOf(kind));

    public static void Set(string kind, byte[] png)
    {
        string? bad = Validate(png);
        if (bad != null) throw new Exception(bad);
        Directory.CreateDirectory(Paths.Root);
        File.WriteAllBytes(FileOf(kind), png);
    }

    public static void Remove(string kind)
    {
        try { if (File.Exists(FileOf(kind))) File.Delete(FileOf(kind)); } catch { }
    }

    public static string? Preview(string kind) =>
        HasKind(kind) ? "data:image/png;base64," + Convert.ToBase64String(File.ReadAllBytes(FileOf(kind))) : null;

    /// <summary>把披風與鞘翅複製到實例 (沒有就刪除舊的)，模組會自己讀取</summary>
    public static void SyncTo(Instance inst)
    {
        string dir = Path.Combine(Paths.InstanceDir(inst.Id), "config");
        foreach (var (kind, name) in new[] { ("cape", "cubenova_cape.png"), ("elytra", "cubenova_elytra.png") })
        {
            string dst = Path.Combine(dir, name);
            try
            {
                if (HasKind(kind))
                {
                    Directory.CreateDirectory(dir);
                    File.Copy(FileOf(kind), dst, true);
                }
                else if (File.Exists(dst)) File.Delete(dst);
            }
            catch { }
        }
    }
}
