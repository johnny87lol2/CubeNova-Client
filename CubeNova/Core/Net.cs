using Newtonsoft.Json.Linq;

namespace CubeNova;

public class DlItem
{
    public string Url = "";
    public string Path = "";
    public long Size = 0;
}

public static class Net
{
    public static readonly HttpClient Http;

    static Net()
    {
        var handler = new HttpClientHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.All,
            AllowAutoRedirect = true,
        };
        Http = new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(10) };
        Http.DefaultRequestHeaders.UserAgent.ParseAdd("CubeNova-Launcher/2.0");
    }

    public static async Task<string> GetString(string url, CancellationToken ct = default)
    {
        Exception? last = null;
        for (int i = 0; i < 3; i++)
        {
            try
            {
                using var resp = await Http.GetAsync(url, ct);
                resp.EnsureSuccessStatusCode();
                return await resp.Content.ReadAsStringAsync(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                last = ex;
                await Task.Delay(400 * (i + 1), ct);
            }
        }
        throw new Exception(L.T("Network request failed: ", "網路請求失敗：") + url + " (" + last?.Message + ")");
    }

    public static async Task<JToken> GetJson(string url, CancellationToken ct = default)
        => JToken.Parse(await GetString(url, ct));

    /// <summary>下載單一檔案 (先寫 .part 再改名)。progress: 0..1</summary>
    public static async Task Download(string url, string dest, Action<double>? progress = null, CancellationToken ct = default)
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(dest)!);
        string part = dest + ".part";
        Exception? last = null;
        for (int attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                using var resp = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
                resp.EnsureSuccessStatusCode();
                long total = resp.Content.Headers.ContentLength ?? -1;
                using (var src = await resp.Content.ReadAsStreamAsync(ct))
                using (var dst = File.Create(part))
                {
                    var buf = new byte[81920];
                    long read = 0;
                    int n;
                    while ((n = await src.ReadAsync(buf, ct)) > 0)
                    {
                        await dst.WriteAsync(buf.AsMemory(0, n), ct);
                        read += n;
                        if (total > 0) progress?.Invoke((double)read / total);
                    }
                }
                File.Move(part, dest, true);
                progress?.Invoke(1);
                return;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                last = ex;
                try { File.Delete(part); } catch { }
                await Task.Delay(500 * (attempt + 1), ct);
            }
        }
        throw new Exception(L.T("Download failed: ", "下載失敗：") + url + " (" + last?.Message + ")");
    }

    /// <summary>平行下載很多檔案。已存在(且大小吻合)的會跳過。</summary>
    /// <param name="tolerateFailures">允許失敗的數量上限 (資源檔用)</param>
    public static async Task DownloadMany(List<DlItem> items, int parallel, Action<int, int>? progress,
        CancellationToken ct, int tolerateFailures = 0)
    {
        int total = items.Count, done = 0, failed = 0;
        string? firstError = null;
        using var gate = new SemaphoreSlim(parallel);
        var tasks = items.Select(async it =>
        {
            await gate.WaitAsync(ct);
            try
            {
                bool have = File.Exists(it.Path) && (it.Size <= 0 || new FileInfo(it.Path).Length == it.Size);
                if (!have)
                {
                    try { await Download(it.Url, it.Path, null, ct); }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        Interlocked.Increment(ref failed);
                        firstError ??= ex.Message;
                    }
                }
                int d = Interlocked.Increment(ref done);
                if (d % 8 == 0 || d == total) progress?.Invoke(d, total);
            }
            finally { gate.Release(); }
        }).ToList();
        await Task.WhenAll(tasks);
        if (failed > tolerateFailures)
            throw new Exception(L.T("Failed to download files: ", "部分檔案下載失敗：") + firstError);
    }
}
