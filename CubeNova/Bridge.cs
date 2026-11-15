using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace CubeNova;

/// <summary>網頁介面 (JS) 與 C# 後端之間的指令處理</summary>
public class Bridge
{
    readonly Window _owner;
    readonly Action<string, object?> _emit;
    readonly Action<bool> _gameRunningChanged;

    readonly Dictionary<string, CancellationTokenSource> _busy = new();
    readonly Dictionary<string, Process> _running = new();

    public static readonly JsonSerializerSettings Json = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
    };

    public Bridge(Window owner, Action<string, object?> emit, Action<bool> gameRunningChanged)
    {
        _owner = owner;
        _emit = emit;
        _gameRunningChanged = gameRunningChanged;
    }

    static string S(JToken a, string k) => (string?)a[k] ?? "";
    static int I(JToken a, string k, int def = 0) => (int?)a[k] ?? def;

    Instance GetInstance(string id) =>
        Store.Current.Instances.FirstOrDefault(i => i.Id == id)
        ?? throw new Exception(L.T("Instance not found.", "找不到這個實例。"));

    // ───── 自訂標題列的視窗控制 ─────
    bool _maxed;
    Rect _restoreRect;

    public void SetMaximized(bool on)
    {
        if (on == _maxed) return;
        if (on)
        {
            _restoreRect = new Rect(_owner.Left, _owner.Top, _owner.Width, _owner.Height);
            var r = WinChrome.WorkArea(_owner);
            _owner.Left = r.Left;
            _owner.Top = r.Top;
            _owner.Width = r.Width;
            _owner.Height = r.Height;
        }
        else
        {
            _owner.Left = _restoreRect.Left;
            _owner.Top = _restoreRect.Top;
            _owner.Width = _restoreRect.Width;
            _owner.Height = _restoreRect.Height;
        }
        _maxed = on;
        _emit("winstate", new { maximized = _maxed });
    }

    public async Task<object?> Handle(string cmd, JToken a)
    {
        var st = Store.Current;
        switch (cmd)
        {
            case "winMin":
                _owner.WindowState = WindowState.Minimized;
                return true;
            case "winMax":
                SetMaximized(!_maxed);
                return true;
            case "winClose":
                _owner.Close();
                return true;
            case "winDrag":
                if (!_maxed) WinChrome.BeginMove(_owner);
                return true;
            case "winResize":
                if (!_maxed) WinChrome.BeginResize(_owner, S(a, "edge"));
                return true;

            case "init":
                return new
                {
                    settings = new
                    {
                        st.Language, st.RamMb, st.JvmArgs, st.HideLauncherOnPlay, st.AutoCheckUpdates,
                        st.SelectedAccount, st.SelectedInstance,
                    },
                    accounts = st.Accounts.Select(AccountDto).ToList(),
                    instances = st.Instances,
                    builtinVersions = BuiltinMods.AvailableVersions(),
                    running = _running.Keys.ToList(),
                    dataDir = Paths.Root,
                    totalRamMb = TotalRamMb(),
                };

            case "checkUpdate":
                return await Updater.Check();

            case "applyUpdate":
            {
                if (_running.Count > 0 || _busy.Count > 0)
                    throw new Exception(L.T("Close the game before updating.", "請先關閉遊戲再更新。"));
                var info = await Updater.Check();
                if (info.Error != "") throw new Exception(info.Error);
                if (!info.Available) throw new Exception(L.T("You are already up to date.", "目前已是最新版本。"));
                await Updater.Apply(info.Latest, (text, pct) => _emit("updateProgress", new { text, pct }), CancellationToken.None);
                // 批次檔會等這個程式結束後再替換檔案
                _ = Application.Current.Dispatcher.BeginInvoke((Action)(() => Application.Current.Shutdown()));
                return true;
            }

            case "saveSettings":
                if (a["language"] != null) st.Language = S(a, "language") == "zh" ? "zh" : "en";
                if (a["ramMb"] != null) st.RamMb = Math.Clamp(I(a, "ramMb"), 512, 65536);
                if (a["jvmArgs"] != null) st.JvmArgs = S(a, "jvmArgs");
                if (a["hideLauncherOnPlay"] != null) st.HideLauncherOnPlay = (bool)a["hideLauncherOnPlay"]!;
                if (a["autoCheckUpdates"] != null) st.AutoCheckUpdates = (bool)a["autoCheckUpdates"]!;
                Store.Save();
                return true;

            case "mcVersions":
            {
                bool snaps = (bool?)a["snapshots"] == true;
                var man = await GameLauncher.GetManifest();
                return (man["versions"] as JArray ?? new JArray())
                    .Where(v => snaps || (string?)v["type"] == "release")
                    .Select(v => new { id = (string?)v["id"], type = (string?)v["type"] })
                    .ToList();
            }

            // ───── 帳號 ─────
            case "addOffline":
            {
                string name = S(a, "name").Trim();
                if (!Regex.IsMatch(name, "^[A-Za-z0-9_]{3,16}$"))
                    throw new Exception(L.T("Username must be 3-16 characters: letters, numbers, underscore.", "名稱需為 3-16 個字元：英文、數字、底線。"));
                var acc = new Account { Type = "offline", Name = name, Uuid = GameLauncher.OfflineUuid(name) };
                st.Accounts.Add(acc);
                st.SelectedAccount = acc.Id;
                Store.Save();
                return AccountDto(acc);
            }

            case "msLogin":
            {
                // 不能直接在 WebView2 的訊息事件裡開另一個 WebView2 視窗，要排到下一輪再開
                Account? acc = null;
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var w = new MsLoginWindow { Owner = _owner };
                    w.ShowDialog();
                    acc = w.Result;
                });
                if (acc == null) return null;
                return AddMicrosoft(acc);
            }

            case "selectAccount":
                st.SelectedAccount = S(a, "id");
                Store.Save();
                return true;

            case "removeAccount":
                st.Accounts.RemoveAll(x => x.Id == S(a, "id"));
                if (st.SelectedAccount == S(a, "id")) st.SelectedAccount = st.Accounts.FirstOrDefault()?.Id ?? "";
                Store.Save();
                return true;

            // ───── 實例 ─────
            case "createInstance":
            {
                string mc = S(a, "mc");
                string loader = "fabric";
                string name = S(a, "name").Trim();
                if (mc == "") throw new Exception(L.T("Pick a Minecraft version.", "請選擇 Minecraft 版本。"));
                if (BuiltinMods.JarFor(mc) == null)
                    throw new Exception(L.T("CubeNova Client does not support this version yet.", "CubeNova 客戶端尚未支援這個版本。"));
                if (name == "") name = "CubeNova " + mc;
                var inst = new Instance { Name = name, Mc = mc, Loader = loader };
                if (loader == "fabric")
                    inst.LoaderVersion = await GameLauncher.LatestFabricLoader(mc, CancellationToken.None);
                st.Instances.Add(inst);
                st.SelectedInstance = inst.Id;
                Directory.CreateDirectory(Paths.InstanceDir(inst.Id));
                Store.Save();
                return inst;
            }

            case "updateInstance":
            {
                var inst = GetInstance(S(a, "id"));
                if (a["name"] != null && S(a, "name").Trim() != "") inst.Name = S(a, "name").Trim();
                if (a["ramMb"] != null) inst.RamMb = Math.Max(0, I(a, "ramMb"));
                if (a["builtin"] != null) inst.BuiltinMods = (bool)a["builtin"]!;
                Store.Save();
                return inst;
            }

            case "deleteInstance":
            {
                var inst = GetInstance(S(a, "id"));
                if (_running.ContainsKey(inst.Id)) throw new Exception(L.T("Close the game first.", "請先關閉遊戲。"));
                st.Instances.Remove(inst);
                if (st.SelectedInstance == inst.Id) st.SelectedInstance = st.Instances.FirstOrDefault()?.Id ?? "";
                Store.Save();
                try { Directory.Delete(Paths.InstanceDir(inst.Id), true); } catch { }
                return true;
            }

            case "selectInstance":
                st.SelectedInstance = S(a, "id");
                Store.Save();
                return true;

            case "openFolder":
            {
                var inst = GetInstance(S(a, "id"));
                string sub = S(a, "sub");
                string dir = Paths.InstanceDir(inst.Id);
                if (sub != "") dir = Path.Combine(dir, Path.GetFileName(sub));
                Directory.CreateDirectory(dir);
                Process.Start(new ProcessStartInfo("explorer.exe", "\"" + dir + "\"") { UseShellExecute = true });
                return true;
            }

            case "openUrl":
            {
                string url = S(a, "url");
                if (url.StartsWith("https://"))
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                return true;
            }

            // ───── 啟動 ─────
            case "launch":
                await Launch(S(a, "id"));
                return true;

            case "stop":
            {
                string id = S(a, "id");
                if (_busy.TryGetValue(id, out var cts)) cts.Cancel();
                if (_running.TryGetValue(id, out var p)) { try { p.Kill(true); } catch { } }
                return true;
            }

            // ───── 模組商店 ─────
            case "search":
            {
                var res = await Modrinth.Search(S(a, "type"), S(a, "query"), S(a, "mc"), S(a, "loader"),
                    I(a, "offset"), S(a, "sort") == "downloads" ? "downloads" : "relevance", CancellationToken.None);
                return new
                {
                    total = (int?)res["total_hits"] ?? 0,
                    hits = (res["hits"] as JArray ?? new JArray()).Select(h => new
                    {
                        id = (string?)h["project_id"],
                        title = (string?)h["title"],
                        description = (string?)h["description"],
                        icon = (string?)h["icon_url"],
                        downloads = (long?)h["downloads"] ?? 0,
                        author = (string?)h["author"],
                    }).ToList(),
                };
            }

            case "installProject":
            {
                var inst = GetInstance(S(a, "instanceId"));
                string type = S(a, "type");
                if (type == "mod" && inst.Loader != "fabric")
                    throw new Exception(L.T("Mods need a Fabric instance.", "模組需要 Fabric 實例。"));
                return await Modrinth.Install(S(a, "projectId"), type, inst, null, CancellationToken.None);
            }

            // ───── 實例檔案 ─────
            case "listFiles":
                return ListFiles(GetInstance(S(a, "id")), S(a, "kind"));

            case "toggleFile":
            {
                string path = FilePath(GetInstance(S(a, "id")), S(a, "kind"), S(a, "name"));
                string dest = path.EndsWith(".disabled") ? path[..^".disabled".Length] : path + ".disabled";
                File.Move(path, dest, true);
                return true;
            }

            case "deleteFile":
                File.Delete(FilePath(GetInstance(S(a, "id")), S(a, "kind"), S(a, "name")));
                return true;

            // ───── 自訂披風 ─────
            case "capeInfo":
                return new { has = Capes.HasKind("cape"), preview = Capes.Preview("cape") ?? "", hasElytra = Capes.HasKind("elytra"), elytraPreview = Capes.Preview("elytra") ?? "" };

            case "capeSave":
            {
                string b64 = S(a, "data");
                int comma = b64.IndexOf(',');
                if (comma >= 0) b64 = b64[(comma + 1)..];
                Capes.Set(S(a, "kind"), Convert.FromBase64String(b64));
                return true;
            }

            case "capeRemove":
                Capes.Remove(S(a, "kind"));
                return true;

            // ───── 內建模組設定 ─────
            case "builtinInfo":
            {
                var inst = GetInstance(S(a, "id"));
                return new
                {
                    available = BuiltinMods.JarFor(inst.Mc) != null,
                    fabric = inst.Loader == "fabric",
                    config = BuiltinMods.ReadConfig(inst),
                };
            }

            case "saveBuiltinConfig":
            {
                var inst = GetInstance(S(a, "id"));
                var cur = BuiltinMods.ReadConfig(inst);
                if (a["config"] is JObject patch) cur.Merge(patch, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace });
                BuiltinMods.WriteConfig(inst, cur);
                return true;
            }
        }
        throw new Exception("Unknown command: " + cmd);
    }

    // ───────────── helpers ─────────────

    object AddMicrosoft(Account acc)
    {
        var st = Store.Current;
        var old = st.Accounts.FirstOrDefault(x => x.Type == "microsoft" && x.Uuid == acc.Uuid);
        if (old != null) { acc.Id = old.Id; st.Accounts.Remove(old); }
        st.Accounts.Add(acc);
        st.SelectedAccount = acc.Id;
        Store.Save();
        return AccountDto(acc);
    }

    static object AccountDto(Account a) => new
    {
        a.Id, a.Type, a.Name, a.Uuid,
        expired = a.Type == "microsoft" && a.Expires < DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
    };

    static long TotalRamMb()
    {
        try { return (long)(GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 1024 / 1024); }
        catch { return 8192; }
    }

    static string KindFolder(string kind) => kind switch
    {
        "mods" => "mods",
        "resourcepacks" => "resourcepacks",
        "shaderpacks" => "shaderpacks",
        _ => throw new Exception("bad kind"),
    };

    static string FilePath(Instance inst, string kind, string name)
    {
        if (Path.GetFileName(name) != name) throw new Exception("bad file name");
        return Path.Combine(Paths.InstanceDir(inst.Id), KindFolder(kind), name);
    }

    static object ListFiles(Instance inst, string kind)
    {
        string dir = Path.Combine(Paths.InstanceDir(inst.Id), KindFolder(kind));
        if (!Directory.Exists(dir)) return new List<object>();
        string ext = kind == "mods" ? ".jar" : ".zip";
        return Directory.GetFiles(dir)
            .Select(f => new FileInfo(f))
            .Where(f => f.Name.EndsWith(ext) || f.Name.EndsWith(ext + ".disabled"))
            .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            .Select(f => new { name = f.Name, size = f.Length, enabled = !f.Name.EndsWith(".disabled") })
            .ToList();
    }

    async Task Launch(string id)
    {
        var st = Store.Current;
        var inst = GetInstance(id);
        if (_running.ContainsKey(id) || _busy.ContainsKey(id))
            throw new Exception(L.T("This instance is already running.", "這個實例已在執行中。"));
        var acc = st.Accounts.FirstOrDefault(x => x.Id == st.SelectedAccount)
            ?? throw new Exception(L.T("Add an account first.", "請先新增帳號。"));
        if (acc.Type == "microsoft" && acc.Expires < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            throw new Exception(L.T("Your Microsoft login expired. Sign in again.", "Microsoft 登入已過期，請重新登入。"));

        var cts = new CancellationTokenSource();
        _busy[id] = cts;
        _emit("state", new { id, state = "installing" });
        void Report(string text, double pct) => _emit("progress", new { id, text, pct });

        try
        {
            var (launchId, java, ver) = await GameLauncher.Prepare(inst, Report, cts.Token);
            Report(L.T("Preparing mods...", "準備模組中..."), 0.95);
            await BuiltinMods.Apply(inst, t => Report(t, 0.96), cts.Token);
            Report(L.T("Starting game...", "啟動遊戲中..."), 1);

            inst.LastPlayed = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            Store.Save();

            var tail = new Queue<string>();
            var proc = GameLauncher.Start(inst, acc, launchId, java, ver, line =>
            {
                lock (tail) { tail.Enqueue(line); while (tail.Count > 80) tail.Dequeue(); }
                _emit("log", new { id, line });
            });
            _running[id] = proc;
            _emit("state", new { id, state = "running" });
            _gameRunningChanged(true);

            proc.Exited += (_, _) =>
            {
                int code = -1;
                try { code = proc.ExitCode; } catch { }
                string tailText;
                lock (tail) tailText = string.Join("\n", tail);
                Application.Current.Dispatcher.BeginInvoke((Action)(() =>
                {
                    _running.Remove(id);
                    _emit("exit", new { id, code, tail = code == 0 ? "" : tailText });
                    _emit("state", new { id, state = "idle" });
                    _gameRunningChanged(false);
                }));
            };
        }
        catch (OperationCanceledException)
        {
            _emit("state", new { id, state = "idle" });
        }
        catch
        {
            _emit("state", new { id, state = "idle" });
            throw;
        }
        finally
        {
            _busy.Remove(id);
        }
    }
}
