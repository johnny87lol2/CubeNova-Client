using System.Diagnostics;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace CubeNova;

public record UpdateInfo(string Current, string Latest, bool Available, string Error, string Url);

/// <summary>
/// 自動更新：
/// 1. 比對程式資料夾的 v.txt 與 GitHub 上的 v.txt，不一樣就代表有新版。
/// 2. 從最新 Release 下載 CubeNova.zip，解壓到暫存資料夾。
/// 3. 寫一個小的 update.bat：等啟動器關閉 → 把新檔案複製過來 → 重新開啟啟動器。
/// </summary>
public static class Updater
{
    public const string RemoteVersionUrl =
        "https://raw.githubusercontent.com/johnny87lol2/CubeNova-Client/refs/heads/main/v.txt";
    public const string PackageUrl =
        "https://github.com/johnny87lol2/CubeNova-Client/releases/latest/download/CubeNova.zip";
    public const string ReleasesUrl = "https://github.com/johnny87lol2/CubeNova-Client/releases/latest";

    static string Clean(string s) => (s ?? "").Trim().Trim('﻿').Trim();

    public static string LocalVersion()
    {
        try
        {
            string p = Path.Combine(Paths.AppDir, "v.txt");
            return File.Exists(p) ? Clean(File.ReadAllText(p)) : "";
        }
        catch { return ""; }
    }

    public static async Task<UpdateInfo> Check()
    {
        string current = LocalVersion();
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            // 加上時間參數避免被 GitHub 的快取擋住
            string url = RemoteVersionUrl + "?t=" + DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string latest = Clean(await Net.GetString(url, cts.Token));
            if (latest == "" || latest.Length > 40 || latest.Contains('<'))
                throw new Exception(L.T("Invalid version file on GitHub.", "GitHub 上的版本檔內容不正確。"));
            return new UpdateInfo(current, latest, latest != current, "", ReleasesUrl);
        }
        catch (Exception ex)
        {
            return new UpdateInfo(current, "", false, ex.Message, ReleasesUrl);
        }
    }

    /// <summary>下載並準備好更新，最後啟動 update.bat。呼叫完成後，呼叫端要立刻關閉程式。</summary>
    public static async Task Apply(string latest, Action<string, double> report, CancellationToken ct)
    {
        if (!Regex.IsMatch(latest, @"^[A-Za-z0-9._-]{1,40}$"))
            throw new Exception(L.T("Unsupported version format.", "不支援的版本格式。"));

        string work = Path.Combine(Path.GetTempPath(), "CubeNovaUpdate");
        try { if (Directory.Exists(work)) Directory.Delete(work, true); } catch { }
        Directory.CreateDirectory(work);

        string zip = Path.Combine(work, "CubeNova.zip");
        report(L.T("Downloading update...", "下載更新中..."), 0);
        await Net.Download(PackageUrl, zip,
            p => report(L.T("Downloading update... ", "下載更新中... ") + (int)(p * 100) + "%", p * 0.8), ct);

        report(L.T("Extracting...", "解壓縮中..."), 0.85);
        string extracted = Path.Combine(work, "new");
        await Task.Run(() => ZipFile.ExtractToDirectory(zip, extracted, true), ct);

        string? exe = Directory.GetFiles(extracted, "CubeNova.exe", SearchOption.AllDirectories).FirstOrDefault();
        if (exe == null)
            throw new Exception(L.T("The update package does not contain CubeNova.exe.", "更新包裡找不到 CubeNova.exe。"));
        string src = Path.GetDirectoryName(exe)!;
        string dst = Paths.AppDir.TrimEnd('\\', '/');
        string self = Environment.ProcessPath ?? Path.Combine(dst, "CubeNova.exe");

        // 批次檔只放 ASCII；路徑透過環境變數傳進去，避免中文路徑亂碼
        string bat = Path.Combine(work, "update.bat");
        File.WriteAllText(bat, string.Join("\r\n", new[]
        {
            "@echo off",
            "set \"LOG=%~dp0update.log\"",
            ":wait",
            "tasklist /FI \"PID eq %CN_PID%\" 2>nul | find \"%CN_PID%\" >nul",
            "if not errorlevel 1 (",
            "  ping -n 2 127.0.0.1 >nul",
            "  goto wait",
            ")",
            "echo Updating > \"%LOG%\"",
            "robocopy \"%CN_SRC%\" \"%CN_DST%\" /E /IS /IT /R:3 /W:1 /NFL /NDL /NJH /NJS /NP >> \"%LOG%\" 2>&1",
            "if errorlevel 8 (",
            "  echo FAILED >> \"%LOG%\"",
            ") else (",
            "  (echo %CN_VER%)>\"%CN_DST%\\v.txt\"",
            ")",
            "start \"\" \"%CN_EXE%\"",
            "",
        }));

        report(L.T("Restarting...", "即將重新啟動..."), 0.95);
        var psi = new ProcessStartInfo("cmd.exe")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = work,
        };
        psi.ArgumentList.Add("/c");
        psi.ArgumentList.Add(bat);
        psi.Environment["CN_PID"] = Environment.ProcessId.ToString();
        psi.Environment["CN_SRC"] = src;
        psi.Environment["CN_DST"] = dst;
        psi.Environment["CN_EXE"] = self;
        psi.Environment["CN_VER"] = latest;
        Process.Start(psi);
    }
}
