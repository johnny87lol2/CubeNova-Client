# CubeNova

[English](README.md) | [繁體中文](README.zh-TW.md)

一個 Windows 用的 Minecraft 啟動器，內建類似 Lunar Client 的 Fabric HUD 客戶端。

啟動器用 C#（.NET 8、WPF + WebView2）撰寫，介面是 HTML/CSS/JS。內建客戶端 **CubeNova Client** 是一個小型 Fabric 模組，啟動器會自動把它裝進你的實例。

> **目前狀態：早期開發。** 只支援 Windows。內建客戶端目前只支援 **Minecraft 1.21.1**。可能還有不少問題，歡迎開 issue 回報。

## 功能

### 啟動器
- **Fabric** 實例，每個實例各有獨立的 `mods`、`resourcepacks`、`shaderpacks` 資料夾
- **自動安裝 Java**：依 Minecraft 版本需求（8 / 17 / 21 / 25 …）從 [Adoptium](https://adoptium.net/) 下載到啟動器自己的資料夾
- **Modrinth 商店**：搜尋並安裝模組（含必要相依）、資源包、光影
- Microsoft 登入與離線帳號
- 英文與繁體中文介面（預設英文）
- 自訂深色視窗與標題列

### CubeNova Client（內建模組）
遊戲中按 **右 Shift** 開啟選單，HUD 元件可以隨意拖曳。

| 模組 | 功能 |
| --- | --- |
| FPS | 每秒幀數 |
| CPS | 每秒點擊數（左 \| 右） |
| Keystrokes | W A S D、滑鼠按鍵與跳躍 |
| Coordinates | XYZ 座標 |
| Direction | 面向的方向 |
| Clock | 現實時間 |
| Night Vision | 本機夜視 |
| Toggle Sprint | 按住前進自動衝刺 |
| Zoom | 按住 **C** 放大。開啟時會自動解除其他使用同一個按鍵的功能 |
| Max Performance | 進入世界時自動關閉垂直同步，並把 FPS 上限設為無限 |

模組也會把標題畫面的 Minecraft 標誌換成 CubeNova Client 標誌。

## 需求

- Windows 10 或 11（x64）
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) 與 Visual Studio 2022 以上（用來建置啟動器）
- [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/)（Windows 11 已內建）
- 使用 Microsoft 登入需要有 Minecraft Java 版的帳號

## 建置

### 1. 啟動器
1. 用 Visual Studio 開啟 `CubeNova.sln`。
2. 按 **F5**。

遊戲檔案與設定存放在 `%APPDATA%\.cubenova`。

### 2. CubeNova Client 模組
啟動器會尋找 `CubeNova/builtin/cubenova-client-<Minecraft 版本>.jar`。沒有 jar 啟動器仍能運作，只是內建客戶端會停用。取得 jar 有兩種方式：

- **本機建置**：執行 `mod\build-mod.bat`（Minecraft 1.21.1，JDK 21）或 `mod-26.2\build-mod.bat`（Minecraft 26.2，JDK 25、Gradle 9.5.1）。它會把 JDK 21 與 Gradle 一次性下載到 `%LOCALAPPDATA%\CubeNovaTools`，建置模組，並把 jar 複製到 `CubeNova\builtin`。
- **GitHub Actions**：推送到 GitHub 後，`.github/workflows/build-mod.yml` 會自動建置。下載 `cubenova-client-jar` 這個 artifact，把 jar 放進 `CubeNova/builtin/`。

放入 jar 之後請重新啟動啟動器。

### 支援其他 Minecraft 版本
修改 `mod/gradle.properties` 裡的 `minecraft_version`、`loader_version`、`fabric_version`（參考 <https://fabricmc.net/develop>），再重新建置。輸出的檔名會是 `cubenova-client-<版本>.jar`，啟動器列出的版本就是 `builtin/` 裡找得到的版本。各版本的遊戲 API 不同，原始碼可能需要為每個版本做小幅修改。

## 更新

啟動器開啟時會比對程式旁邊的 `v.txt` 與 [`main` 分支上的 `v.txt`](https://raw.githubusercontent.com/johnny87lol2/CubeNova-Client/refs/heads/main/v.txt)（可在設定中關閉這個檢查）。兩邊內容不同就會跳出「有新版本」的提醒。按下**立即更新**會從[最新 Release](https://github.com/johnny87lol2/CubeNova-Client/releases/latest) 下載 `CubeNova.zip`，關閉啟動器、替換程式檔案，再自動重新開啟。你的帳號、實例和設定都在 `%APPDATA%\.cubenova`，不會被動到。

### 發布新版本
1. 修改 repo 根目錄 `v.txt` 的數字（例如 `2`）並 commit。
2. 推送名稱為 `v` + 該數字的 tag：`git tag v2 && git push origin v2`。
3. `Release` 工作流程會建置模組與啟動器，並把 `CubeNova.zip` 附加到新的 GitHub Release。如果 `v.txt` 和 tag 不一致，它會失敗。

更新包必須是 zip，裡面有 `CubeNova.exe` 以及其他發布檔案（`dotnet publish` 的輸出加上 `builtin/`）。更新程式只會透過 HTTPS 從這個 repo 的 Release 下載，沒有驗證簽章。

## 專案結構

```
CubeNova/            WPF 啟動器 (C#)
  Core/              下載、Java、Fabric、啟動遊戲、Modrinth
  web/               HTML/CSS/JS 介面
  builtin/           把 cubenova-client-<版本>.jar 放在這裡
mod/                 Fabric 模組原始碼 1.21.1 (Java 21)
mod-1.21.4/          Fabric 模組原始碼 1.21.4 (Java 21)
mod-26.2/            Fabric 模組原始碼 26.2 (Java 25)
.github/workflows/   自動建置模組的 CI
```

## 注意事項

- **Microsoft 登入**使用的是 Minecraft 啟動器歷來使用的公開 client ID。Microsoft 隨時可能改變這個機制；如果登入壞掉，請開 issue 回報。
- **離線帳號**是給測試與離線伺服器使用的。請購買遊戲以支持 Mojang。
- **Max Performance** 模組會直接修改遊戲自己的垂直同步與 FPS 上限設定（存在 `options.txt`）。
- CubeNova 是非官方專案，**與 Mojang Studios、Microsoft、Lunar Client 沒有任何隸屬、背書或關聯**。「Minecraft」是 Mojang Studios 的商標。

## 貢獻

歡迎提交 issue 與 pull request，請說明你改了什麼以及如何測試。

## 授權

[MIT](LICENSE)
