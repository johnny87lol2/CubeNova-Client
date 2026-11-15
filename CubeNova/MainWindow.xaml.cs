using System.Windows;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CubeNova;

public partial class MainWindow : Window
{
    Bridge? _bridge;

    public MainWindow()
    {
        InitializeComponent();
        Web.DefaultBackgroundColor = System.Drawing.Color.FromArgb(11, 13, 18);
        Store.Load();
        StateChanged += (_, _) =>
        {
            // 若被系統最大化 (例如 Win+上)，改用我們自己的最大化，避免蓋住工作列/邊緣被切掉
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                _bridge?.SetMaximized(true);
            }
        };
        Loaded += async (_, _) => await InitAsync();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        WinChrome.Dark(this);
    }

    async Task InitAsync()
    {
        try
        {
            await Web.EnsureCoreWebView2Async(await WebEnv.Get());
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "WebView2 Runtime is required / 需要安裝 WebView2 Runtime:\nhttps://developer.microsoft.com/microsoft-edge/webview2/\n\n" + ex.Message,
                "CubeNova");
            Close();
            return;
        }

        var core = Web.CoreWebView2;
        core.Settings.AreDefaultContextMenusEnabled = false;
        core.Settings.IsStatusBarEnabled = false;
        core.Settings.AreDevToolsEnabled = true; // F12 除錯用
        core.SetVirtualHostNameToFolderMapping("app.cubenova", Paths.WebDir,
            CoreWebView2HostResourceAccessKind.Allow);

        _bridge = new Bridge(this, Emit, running =>
        {
            if (Store.Current.HideLauncherOnPlay)
                WindowState = running ? WindowState.Minimized : WindowState.Normal;
        });

        core.WebMessageReceived += OnMessage;
        core.NavigationStarting += (_, e) =>
        {
            // 外部連結用預設瀏覽器開，不要在啟動器內導覽
            if (!e.Uri.StartsWith("https://app.cubenova/"))
            {
                e.Cancel = true;
                if (e.Uri.StartsWith("https://"))
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri) { UseShellExecute = true });
            }
        };
        core.NewWindowRequested += (_, e) =>
        {
            e.Handled = true;
            if (e.Uri.StartsWith("https://"))
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri) { UseShellExecute = true });
        };
        core.Navigate("https://app.cubenova/index.html");
    }

    /// <summary>送事件給網頁 (任何執行緒都可呼叫)</summary>
    void Emit(string name, object? data)
    {
        string json = JsonConvert.SerializeObject(new { @event = name, data }, Bridge.Json);
        Dispatcher.BeginInvoke((Action)(() =>
        {
            try { Web.CoreWebView2?.PostWebMessageAsJson(json); } catch { }
        }));
    }

    async void OnMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        string id = "";
        try
        {
            var msg = JObject.Parse(e.WebMessageAsJson);
            id = (string?)msg["id"] ?? "";
            string cmd = (string?)msg["cmd"] ?? "";
            var args = msg["args"] ?? new JObject();
            var result = await _bridge!.Handle(cmd, args);
            Reply(new { id, ok = true, data = result });
        }
        catch (Exception ex)
        {
            Reply(new { id, ok = false, error = ex.Message });
        }
    }

    void Reply(object o)
    {
        try { Web.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(o, Bridge.Json)); } catch { }
    }
}
