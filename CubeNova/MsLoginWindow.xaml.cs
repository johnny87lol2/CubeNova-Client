using System.Text;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CubeNova;

public static class WebEnv
{
    static Task<CoreWebView2Environment>? _env;
    public static Task<CoreWebView2Environment> Get()
    {
        Directory.CreateDirectory(Paths.WebView);
        return _env ??= CoreWebView2Environment.CreateAsync(null, Paths.WebView);
    }
}

public partial class MsLoginWindow : Window
{
    const string ClientId = "00000000402b5328";
    const string Redirect = "https://login.live.com/oauth20_desktop.srf";

    public static string LoginUrl =>
        "https://login.microsoftonline.com/consumers/oauth2/v2.0/authorize" +
        $"?client_id={ClientId}&response_type=token" +
        $"&redirect_uri={Uri.EscapeDataString(Redirect)}" +
        "&scope=XboxLive.signin%20offline_access&prompt=select_account";

    public Account? Result { get; private set; }
    bool _done;

    public MsLoginWindow()
    {
        InitializeComponent();
        Title = L.T("Sign in with Microsoft", "使用 Microsoft 登入");
        StatusText.Text = Title;
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
            await Browser.EnsureCoreWebView2Async(await WebEnv.Get());
            Browser.CoreWebView2.CookieManager.DeleteAllCookies();
            string url = LoginUrl;
            Browser.CoreWebView2.NavigationCompleted += (_, e) =>
            {
                if (!e.IsSuccess && !_done)
                    StatusText.Text = L.T("Page failed to load: ", "頁面載入失敗：") + e.WebErrorStatus;
            };
            Browser.CoreWebView2.Navigate(url);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "WebView2");
            Close();
        }
    }

    async void Browser_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        if (_done) return;
        string url = e.Uri ?? "";
        if (!url.StartsWith(Redirect)) return;
        e.Cancel = true;
        _done = true;

        string msaToken = "";
        var uri = new Uri(url);
        string combined = uri.Fragment.TrimStart('#') + "&" + uri.Query.TrimStart('?');
        foreach (var part in combined.Split('&'))
        {
            var kv = part.Split('=', 2);
            if (kv.Length == 2 && kv[0] == "access_token") { msaToken = Uri.UnescapeDataString(kv[1]); break; }
        }
        if (string.IsNullOrEmpty(msaToken))
        {
            MessageBox.Show(L.T("Login was cancelled or no token was returned.", "登入已取消或沒有取得 token。"));
            Close();
            return;
        }

        StatusText.Text = L.T("Verifying account...", "驗證帳號中...");
        try
        {
            Result = await AuthChain(msaToken);
            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, L.T("Login failed", "登入失敗"));
            Close();
        }
    }

    static async Task<JObject> PostJson(string url, object body, string? bearer = null)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json")
        };
        req.Headers.Accept.ParseAdd("application/json");
        if (bearer != null) req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearer);
        using var resp = await Net.Http.SendAsync(req);
        string text = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode && !text.Contains("XErr"))
            throw new Exception($"{url} → {(int)resp.StatusCode}: {text}");
        return JObject.Parse(text);
    }

    public static async Task<Account> AuthChain(string msaToken)
    {
        // MSA → Xbox Live
        var xbl = await PostJson("https://user.auth.xboxlive.com/user/authenticate", new
        {
            Properties = new { AuthMethod = "RPS", SiteName = "user.auth.xboxlive.com", RpsTicket = "d=" + msaToken },
            RelyingParty = "http://auth.xboxlive.com",
            TokenType = "JWT",
        });
        string xblToken = (string)xbl["Token"]!;
        string uhs = (string)xbl["DisplayClaims"]!["xui"]![0]!["uhs"]!;

        // Xbox Live → XSTS
        var xsts = await PostJson("https://xsts.auth.xboxlive.com/xsts/authorize", new
        {
            Properties = new { SandboxId = "RETAIL", UserTokens = new[] { xblToken } },
            RelyingParty = "rp://api.minecraftservices.com/",
            TokenType = "JWT",
        });
        if (xsts["XErr"] != null)
        {
            long xerr = (long)xsts["XErr"]!;
            string msg = xerr switch
            {
                2148916233 => L.T("This Microsoft account has no Xbox account. Create one at xbox.com first.", "這個 Microsoft 帳號沒有 Xbox 帳號，請先到 xbox.com 建立。"),
                2148916235 => L.T("Xbox Live is not available in your country/region.", "你的國家/地區無法使用 Xbox Live。"),
                2148916238 => L.T("This is a child account; add it to a family first.", "這是兒童帳號，需先加入家庭群組。"),
                _ => "XSTS error " + xerr,
            };
            throw new Exception(msg);
        }
        string xstsToken = (string)xsts["Token"]!;

        // XSTS → Minecraft
        var mc = await PostJson("https://api.minecraftservices.com/authentication/login_with_xbox",
            new { identityToken = $"XBL3.0 x={uhs};{xstsToken}" });
        string mcToken = (string?)mc["access_token"] ?? throw new Exception(L.T("Minecraft login failed.", "Minecraft 登入失敗。"));
        long expiresIn = (long?)mc["expires_in"] ?? 86400;

        // 個人資料 (沒買遊戲會 404)
        using var preq = new HttpRequestMessage(HttpMethod.Get, "https://api.minecraftservices.com/minecraft/profile");
        preq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", mcToken);
        using var presp = await Net.Http.SendAsync(preq);
        if (!presp.IsSuccessStatusCode)
            throw new Exception(L.T("This account does not own Minecraft: Java Edition.", "這個帳號沒有 Minecraft Java 版。"));
        var profile = JObject.Parse(await presp.Content.ReadAsStringAsync());

        return new Account
        {
            Type = "microsoft",
            Name = (string)profile["name"]!,
            Uuid = (string)profile["id"]!,
            Token = mcToken,
            Expires = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + expiresIn - 60,
        };
    }
}
