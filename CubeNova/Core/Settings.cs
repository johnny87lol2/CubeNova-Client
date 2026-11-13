using Newtonsoft.Json;

namespace CubeNova;

public class Account
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    /// <summary>"offline" 或 "microsoft"</summary>
    public string Type { get; set; } = "offline";
    public string Name { get; set; } = "";
    public string Uuid { get; set; } = "";
    public string Token { get; set; } = "";
    /// <summary>Microsoft token 到期時間 (unix 秒)</summary>
    public long Expires { get; set; }
}

public class Instance
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..10];
    public string Name { get; set; } = "";
    public string Mc { get; set; } = "";
    /// <summary>"vanilla" 或 "fabric"</summary>
    public string Loader { get; set; } = "vanilla";
    public string LoaderVersion { get; set; } = "";
    /// <summary>0 = 使用全域設定</summary>
    public int RamMb { get; set; } = 0;
    public bool BuiltinMods { get; set; } = true;
    public long Created { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    public long LastPlayed { get; set; } = 0;
}

public class Settings
{
    public string Language { get; set; } = "en";
    public int RamMb { get; set; } = 3072;
    public string JvmArgs { get; set; } = "";
    public bool HideLauncherOnPlay { get; set; } = false;
    public string SelectedAccount { get; set; } = "";
    public string SelectedInstance { get; set; } = "";
    public List<Account> Accounts { get; set; } = new();
    public List<Instance> Instances { get; set; } = new();
}

public static class Store
{
    static readonly object Lock = new();
    public static Settings Current { get; private set; } = new();

    public static void Load()
    {
        lock (Lock)
        {
            try
            {
                if (File.Exists(Paths.SettingsFile))
                    Current = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(Paths.SettingsFile)) ?? new Settings();
            }
            catch
            {
                try { File.Copy(Paths.SettingsFile, Paths.SettingsFile + ".bad", true); } catch { }
                Current = new Settings();
            }
        }
    }

    public static void Save()
    {
        lock (Lock)
        {
            string tmp = Paths.SettingsFile + ".tmp";
            File.WriteAllText(tmp, JsonConvert.SerializeObject(Current, Formatting.Indented));
            File.Move(tmp, Paths.SettingsFile, true);
        }
    }
}

/// <summary>簡易雙語訊息 (給後端錯誤訊息用)</summary>
public static class L
{
    public static string T(string en, string zh) => Store.Current.Language == "zh" ? zh : en;
}
