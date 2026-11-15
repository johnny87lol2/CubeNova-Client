# CubeNova


A Minecraft launcher for Windows with a built-in, Lunar-style HUD client for Fabric.

The launcher is written in C# (.NET 8, WPF + WebView2) with an HTML/CSS/JS interface. The built-in client, **CubeNova Client**, is a small Fabric mod that the launcher installs into your instances automatically.

> **Status: early development.** Windows only. The built-in client currently targets **Minecraft 1.21.1 Minecraft 1.21.4 Minecraft 26.2** only. Expect rough edges and please open issues or join my discord give me some advice https://discord.gg/sC4pcvq3zj.

## Features

### Launcher
- Instances for **Fabric** with a separate `mods`, `resourcepacks` and `shaderpacks` folder per instance
- **Automatic Java**: downloads the Java version each Minecraft version needs (8 / 17 / 21 / 25 ...) from [Adoptium](https://adoptium.net/) into the launcher's own folder
- **Modrinth store**: search and install mods (with required dependencies), resource packs and shaders
- Microsoft sign-in and offline accounts
- English and Traditional Chinese interface (English by default)
- Custom dark window with its own title bar

### CubeNova Client (built-in mod)
Open the menu in game with **Right Shift**; HUD elements can be dragged anywhere.

| Module | What it does |
| --- | --- |
| FPS | Frames per second counter |
| CPS | Clicks per second (left \| right) |
| Keystrokes | W A S D, mouse buttons and jump |
| Coordinates | XYZ position |
| Direction | Facing direction |
| Clock | Real time |
| Night Vision | Client-side night vision |
| Toggle Sprint | Hold forward to sprint automatically |
| Zoom | Hold **C** to zoom. Enabling it unbinds other actions that use the same key |
| Freelook | Hold **Left Alt** to look around without turning your player |
| Custom capes | Other players cannot see it |
| Max Performance | On joining a world: VSync off and FPS limit set to unlimited |

The mod also replaces the Minecraft logo on the title screen with a CubeNova Client logo.

## Requirements

- Windows 10 or 11 (x64)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) and Visual Studio 2022 or newer (to build the launcher)
- [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/) (already included in Windows 11)
- A Minecraft: Java Edition account for Microsoft sign-in

## Build

### 1. The launcher
1. Open `CubeNova.sln` in Visual Studio.
2. Press **F5**.

Game files and settings are stored in `%APPDATA%\.cubenova`.

### 2. The CubeNova Client mod
The launcher looks for `CubeNova/builtin/cubenova-client-<minecraft version>.jar`. Without a jar it still works, but the built-in client is disabled. Build the jar in one of two ways:

- **Locally:** run `mod\build-mod.bat` (Minecraft 1.21.1, JDK 21) and/or `mod-26.2\build-mod.bat` (Minecraft 26.2, JDK 25, Gradle 9.5.1). It downloads JDK 21 and Gradle once into `%LOCALAPPDATA%\CubeNovaTools`, builds the mod, and copies the jar into `CubeNova\builtin`.
- **GitHub Actions:** push the repository; the workflow in `.github/workflows/build-mod.yml` builds the mod. Download the `cubenova-client-jar` artifact and put the jar into `CubeNova/builtin/`.

Restart the launcher after adding a jar.

### Supporting another Minecraft version
Edit `minecraft_version`, `loader_version` and `fabric_version` in `mod/gradle.properties` (see <https://fabricmc.net/develop>), then rebuild. The jar name becomes `cubenova-client-<version>.jar`, and the launcher lists exactly the versions it finds in `builtin/`. Game APIs change between versions, so the source may need small changes for each one.

## Updates

On startup the launcher compares the `v.txt` file next to the program with [`v.txt` on the `main` branch](https://raw.githubusercontent.com/johnny87lol2/CubeNova-Client/refs/heads/main/v.txt) (this check can be turned off in Settings). If the two values differ, it shows an "update available" dialog. **Update now** downloads `CubeNova.zip` from the [latest release](https://github.com/johnny87lol2/CubeNova-Client/releases/latest), closes the launcher, replaces the program files, and starts it again. Your accounts, instances and settings live in `%APPDATA%\.cubenova` and are not touched.

### Publishing a new version
1. Change the number in `v.txt` at the repository root (for example `2`) and commit.
2. Push a tag whose name is `v` + that number: `git tag v2 && git push origin v2`.
3. The `Release` workflow builds the mod and the launcher and attaches `CubeNova.zip` to a new GitHub release. It fails if `v.txt` and the tag disagree.

The update package must be a zip with `CubeNova.exe` and the rest of the published files (what `dotnet publish` produces plus `builtin/`). The updater only downloads from this repository's releases over HTTPS and does not verify a signature.

## Project layout

```
CubeNova/            WPF launcher (C#)
  Core/              downloads, Java, Fabric, game launch, Modrinth
  web/               HTML/CSS/JS interface
  builtin/           put cubenova-client-<version>.jar here
mod/                 Fabric mod source for 1.21.1 (Java 21)
mod-1.21.4/          Fabric mod source for 1.21.4 (Java 21)
mod-26.2/            Fabric mod source for 26.2 (Java 25)
.github/workflows/   CI that builds the mod
```

## Notes

- **Microsoft sign-in** uses the public client ID that Minecraft launchers have historically used. Microsoft can change how this works at any time; if sign-in breaks, please open an issue.
- **Offline accounts** are meant for testing and offline servers. Please buy the game to support Mojang.
- The **Max Performance** module changes the game's own VSync and FPS-limit options (stored in `options.txt`).
- CubeNova is an unofficial project. It is **not affiliated with, endorsed by, or associated with Mojang Studios, Microsoft, or Lunar Client**. "Minecraft" is a trademark of Mojang Studios.

## Contributing

Issues and pull requests are welcome. Please describe what you changed and how you tested it.

